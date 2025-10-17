using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.ViewModels;
using Microsoft.AspNetCore.Identity;
using Wellness_Wardens_Project.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;

namespace Wellness_Wardens_Project.Controllers
{
    [Authorize(Roles = "Stock Manager")]
    public class ConsumablesRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> _userManager;

        public ConsumablesRequestController(ApplicationDbContext context, UserManager<Employee> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET:ViewInventory
        public async Task<IActionResult> ViewInventory()
        {
            var consumables = await _context.Consumables
                .Include(c => c.Ward)
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Ward != null ? c.Ward.Name : "Unknown")
                .ThenBy(c => c.Name)
                .Select(c => new ConsumableViewModel
                {
                    ConsumableId = c.ConsumableId,
                    Name = c.Name,
                    QuantityAvailable = c.QuantityAvailable,
                    WardName = c.Ward != null ? c.Ward.Name : "Unknown Ward",
                    StockStatus = c.QuantityAvailable == 0 ? "Out of Stock" :
                                 c.QuantityAvailable < 5 ? "Critical" :
                                 c.QuantityAvailable < 20 ? "Low Stock" : "In Stock",
                    StatusColor = c.QuantityAvailable == 0 ? "danger" :
                                 c.QuantityAvailable < 10 ? "warning" :
                                 c.QuantityAvailable < 20 ? "info" : "success",
                    IsCritical = c.QuantityAvailable < 5
                })
                .ToListAsync();

            // Calculate statistics for the view
            ViewBag.TotalConsumables = consumables.Count;
            ViewBag.TotalQuantity = consumables.Sum(c => c.QuantityAvailable);
            ViewBag.InStockCount = consumables.Count(c => c.QuantityAvailable >= 20);
            ViewBag.LowStockCount = consumables.Count(c => c.QuantityAvailable < 10 && c.QuantityAvailable >= 5);
            ViewBag.CriticalCount = consumables.Count(c => c.QuantityAvailable < 5 && c.QuantityAvailable > 0);
            ViewBag.OutOfStockCount = consumables.Count(c => c.QuantityAvailable == 0);

            return View(consumables);
        }

        // GET:WeeklyStockTake
        public async Task<IActionResult> WeeklyStockTake()
        {
            var consumables = await _context.Consumables
                .Include(c => c.Ward) 
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Ward != null ? c.Ward.Name : "Unknown")
                .ThenBy(c => c.Name)
                .Select(c => new ConsumableViewModel
                {
                    ConsumableId = c.ConsumableId,
                    Name = c.Name,
                    QuantityAvailable = c.QuantityAvailable,
                    WardName = c.Ward != null ? c.Ward.Name : "Unknown Ward",
                    StockStatus = c.QuantityAvailable == 0 ? "Out of Stock" :
                                 c.QuantityAvailable < 5 ? "Critical" :
                                 c.QuantityAvailable < 20 ? "Low Stock" : "In Stock",
                    StatusColor = c.QuantityAvailable == 0 ? "danger" :
                                 c.QuantityAvailable < 10 ? "warning" :
                                 c.QuantityAvailable < 20 ? "info" : "success",
                    IsCritical = c.QuantityAvailable < 5
                })
                .ToListAsync();

            // Calculate statistics for the report
            var totalItems = consumables.Count;
            var lowStockCount = consumables.Count(c => c.QuantityAvailable < 20);
            var criticalCount = consumables.Count(c => c.QuantityAvailable < 5);
            var outOfStockCount = consumables.Count(c => c.QuantityAvailable == 0);

            ViewBag.TotalItems = totalItems;
            ViewBag.LowStockCount = lowStockCount;
            ViewBag.CriticalCount = criticalCount;
            ViewBag.OutOfStockCount = outOfStockCount;
            ViewBag.ReportDate = DateTime.Now.ToString("yyyy-MM-dd");

            return View(consumables);
        }

        // GET:Create
        public async Task<IActionResult> Create(int? consumableId = null)
        {
            var viewModel = await CreateConsumableRequestViewModel();

            // If consumable is pre-selected auto-fill the form
            if (consumableId.HasValue)
            {
                var consumable = await _context.Consumables
                    .Include(c => c.Ward)
                    .FirstOrDefaultAsync(c => c.ConsumableId == consumableId && !c.IsDeleted);

                if (consumable != null && consumable.WardId.HasValue)
                {
                    viewModel.SelectedConsumableId = consumable.ConsumableId;
                    viewModel.SelectedWardId = consumable.WardId.Value;
                    viewModel.QuantityRequested = Math.Max(20 - consumable.QuantityAvailable, 1);

                    TempData["InfoMessage"] = $"Form pre-filled with {consumable.Name} from {consumable.Ward?.Name}. Suggested quantity: {viewModel.QuantityRequested}";
                }
            }

            return View(viewModel);
        }

        // POST:Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateConsumableRequestViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    var selectedConsumable = await _context.Consumables
                        .Include(c => c.Ward)
                        .FirstOrDefaultAsync(c => c.ConsumableId == viewModel.SelectedConsumableId && !c.IsDeleted);

                    if (selectedConsumable == null)
                    {
                        ModelState.AddModelError("", "Selected consumable not found.");
                        await PopulateCreateViewModel(viewModel);
                        return View(viewModel);
                    }

                    var user = await _userManager.GetUserAsync(User);
                    if (user == null)
                    {
                        TempData["ErrorMessage"] = "User not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Create the request
                    var request = new ConsumablesRequest
                    {
                        ConsumableName = selectedConsumable.Name,
                        QuantityRequested = viewModel.QuantityRequested,
                        RequestedDate = DateTime.Now,
                        IsDelivered = false,
                        IsDeleted = false,
                        WardId = selectedConsumable.WardId,
                        EmployeeId = user.Id
                    };

                    _context.Add(request);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Request created successfully for {selectedConsumable.Name} in {selectedConsumable.Ward?.Name}!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error creating request: {ex.Message}";
                    await PopulateCreateViewModel(viewModel);
                    return View(viewModel);
                }
            }

            await PopulateCreateViewModel(viewModel);
            return View(viewModel);
        }

        // GET: Use low stock suggestion
        public async Task<IActionResult> UseSuggestion(int consumableId)
        {
            var consumable = await _context.Consumables
                .Include(c => c.Ward)
                .FirstOrDefaultAsync(c => c.ConsumableId == consumableId && !c.IsDeleted);

            if (consumable == null)
            {
                TempData["ErrorMessage"] = "Consumable not found.";
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Create), new { consumableId = consumableId });
        }

        // GET: Get Ward for selected consumable
        public async Task<JsonResult> GetWardForConsumable(int consumableId)
        {
            var consumable = await _context.Consumables
                .Include(c => c.Ward)
                .FirstOrDefaultAsync(c => c.ConsumableId == consumableId && !c.IsDeleted);

            if (consumable == null || consumable.Ward == null)
            {
                return Json(new { success = false, message = "Consumable or ward not found." });
            }

            return Json(new
            {
                success = true,
                wardId = consumable.WardId,
                wardName = consumable.Ward.Name,
                currentQuantity = consumable.QuantityAvailable,
                suggestedQuantity = Math.Max(20 - consumable.QuantityAvailable, 1)
            });
        }

        // GET: Mark request as delivered
        public async Task<IActionResult> MarkDelivered(int id)
        {
            var request = await _context.ConsumablesRequests
                .Include(r => r.Employee)
                .Include(r => r.Ward)
                .FirstOrDefaultAsync(r => r.RequestId == id && !r.IsDeleted);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(request);
        }

        // POST: Confirm delivery
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDeliveredConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.ConsumablesRequests
                    .Include(r => r.Ward)
                    .FirstOrDefaultAsync(r => r.RequestId == id && !r.IsDeleted);

                if (request == null)
                {
                    TempData["ErrorMessage"] = "Request not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (request.IsDelivered)
                {
                    TempData["WarningMessage"] = "This request has already been delivered.";
                    return RedirectToAction(nameof(Index));
                }

                // Update request status
                request.IsDelivered = true;
                _context.Update(request);

                var consumable = await _context.Consumables
                    .Include(c => c.Ward)
                    .FirstOrDefaultAsync(c => !c.IsDeleted &&
                                            c.WardId == request.WardId &&
                                            c.Name.ToLower() == request.ConsumableName.ToLower());

                if (consumable != null)
                {
                    var oldQuantity = consumable.QuantityAvailable;
                    var wasLowStock = oldQuantity < 20; 

                    consumable.QuantityAvailable += request.QuantityRequested;
                    consumable.EmployeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    consumable.RequestId = request.RequestId;
                    _context.Update(consumable);

                    TempData["SuccessMessage"] = $"Request delivered! {consumable.Name} stock updated from {oldQuantity} → {consumable.QuantityAvailable} in {consumable.Ward?.Name}.";

                   
                    if (wasLowStock && consumable.QuantityAvailable >= 20)
                    {
                        TempData["InfoMessage"] = $"{consumable.Name} is no longer considered low stock.";
                    }
                }
                else
                {
                    // If consumable doesn't exist in that ward, create it
                    var newConsumable = new Consumable
                    {
                        Name = request.ConsumableName,
                        QuantityAvailable = request.QuantityRequested,
                        WardId = request.WardId,
                        EmployeeId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                        RequestId = request.RequestId,
                        IsDeleted = false
                    };

                    _context.Add(newConsumable);
                    TempData["SuccessMessage"] = $"Request delivered and new consumable '{request.ConsumableName}' added to inventory for {request.Ward?.Name ?? "the ward"}.";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error updating delivery: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Index
        public async Task<IActionResult> Index()
        {
            var requests = await _context.ConsumablesRequests
                .Include(r => r.Employee)
                .Include(r => r.Ward)
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.RequestedDate)
                .Select(r => new ConsumableRequestViewModel
                {
                    RequestId = r.RequestId,
                    ConsumableName = r.ConsumableName,
                    QuantityRequested = r.QuantityRequested,
                    RequestedDate = r.RequestedDate,
                    RequestedBy = $"{r.Employee.FirstName} {r.Employee.LastName}",
                    WardName = r.Ward.Name,
                    IsDelivered = r.IsDelivered,
                    Status = r.IsDelivered ? "Delivered" : "Pending"
                })
                .ToListAsync();

            return View(requests);
        }

        private async Task<CreateConsumableRequestViewModel> CreateConsumableRequestViewModel()
        {
            return new CreateConsumableRequestViewModel
            {
                RequestedDate = DateTime.Now,
                AvailableConsumables = await GetConsumablesSelectList(),
                AvailableWards = await GetWardsSelectList(),
                LowStockSuggestions = await GetLowStockSuggestions()
            };
        }

        private async Task PopulateCreateViewModel(CreateConsumableRequestViewModel viewModel)
        {
            viewModel.AvailableConsumables = await GetConsumablesSelectList();
            viewModel.AvailableWards = await GetWardsSelectList();
            viewModel.LowStockSuggestions = await GetLowStockSuggestions();
        }

        private async Task<List<SelectListItem>> GetConsumablesSelectList()
        {
            return await _context.Consumables
                .Include(c => c.Ward)
                .Where(c => !c.IsDeleted)
                .Select(c => new SelectListItem
                {
                    Value = c.ConsumableId.ToString(),
                    Text = c.QuantityAvailable <= 0
                        ? $"{c.Name} (Ward: {c.Ward.Name}, ⚠️ OUT OF STOCK)"
                        : $"{c.Name} (Ward: {c.Ward.Name}, Available: {c.QuantityAvailable})"
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetWardsSelectList()
        {
            return await _context.Wards
                .Where(w => !w.IsDeleted)
                .Select(w => new SelectListItem
                {
                    Value = w.WardId.ToString(),
                    Text = w.Name
                })
                .ToListAsync();
        }

        private async Task<List<LowStockSuggestionViewModel>> GetLowStockSuggestions()
        {
            return await _context.Consumables
                .Include(c => c.Ward)
                .Where(c => c.QuantityAvailable < 20 && !c.IsDeleted)
                .OrderBy(c => c.QuantityAvailable)
                .Select(c => new LowStockSuggestionViewModel
                {
                    ConsumableId = c.ConsumableId,
                    Name = c.Name,
                    QuantityAvailable = c.QuantityAvailable,
                    WardName = c.Ward.Name,
                    WardId = c.WardId ?? 0,
                    SuggestedQuantity = Math.Max(20 - c.QuantityAvailable, 5) 
                })
                .ToListAsync();
        }
    }
}