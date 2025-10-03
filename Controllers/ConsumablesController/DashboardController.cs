using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.ViewModels;

namespace Wellness_Wardens_Project.Controllers.ConsumablesController
{
    [Authorize(Roles = "Script Manager,Stock Manager")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<Employee> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Script Manager"))
            {
                return RedirectToAction("ScriptManager");
            }
            else if (roles.Contains("Stock Manager"))
            {
                return RedirectToAction("StockManager");
            }

            return RedirectToAction("AccessDenied", "Account");
        }

        public async Task<IActionResult> ScriptManager()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewData["WelcomeName"] = $"{user.Title} {user.LastName}";
            }
            else
            {
                ViewData["WelcomeName"] = User.Identity?.Name ?? "User";
            }
            var model = new ScriptManagerDashboardViewModel();

            // Get pending prescriptions from database
            var pendingPrescriptionsDb = await _context.Prescriptions
                .Include(p => p.Employee)
                .Include(p => p.PrescriptionMedications)
                .Where(p => !p.IsProcessed && !p.IsDeleted)
                .OrderByDescending(p => p.DateWritten)
                .Take(5)
                .ToListAsync();

            // Get recently processed prescriptions
            var recentProcessedDb = await _context.Prescriptions
                .Include(p => p.Employee)
                .Include(p => p.PrescriptionMedications)
                .Where(p => p.IsProcessed && !p.IsDelivered && !p.IsDeleted)
                .OrderByDescending(p => p.DateWritten)
                .Take(5)
                .ToListAsync();

            // Map to ViewModel
            var pendingPrescriptions = pendingPrescriptionsDb.Select(p => new PrescriptionViewModel
            {
                PrescriptionId = p.PrescriptionId,
                DateWritten = p.DateWritten,
                DoctorName = $"{p.Employee?.FirstName} {p.Employee?.LastName}",
                MedicationCount = p.PrescriptionMedications?.Count ?? 0,
                IsProcessed = p.IsProcessed,
                IsDelivered = p.IsDelivered,
                Status = "Pending",
                StatusColor = "warning"
            }).ToList();

            var recentProcessed = recentProcessedDb.Select(p => new PrescriptionViewModel
            {
                PrescriptionId = p.PrescriptionId,
                DateWritten = p.DateWritten,
                DoctorName = $"{p.Employee?.FirstName} {p.Employee?.LastName}",
                MedicationCount = p.PrescriptionMedications?.Count ?? 0,
                IsProcessed = p.IsProcessed,
                IsDelivered = p.IsDelivered,
                Status = p.IsDelivered ? "Delivered" : "In Pharmacy",
                StatusColor = p.IsDelivered ? "success" : "info"
            }).ToList();

            // Calculate statistics
            var pendingCount = await _context.Prescriptions.CountAsync(p => !p.IsProcessed && !p.IsDeleted);
            var processedCount = recentProcessed.Count;

            var completedToday = await _context.Prescriptions
    .CountAsync(p => p.IsDelivered &&
                    p.DateDelivered.HasValue &&
                    p.DateDelivered.Value.Date == DateTime.Today &&
                    !p.IsDeleted);

            // Create ViewModel
            var viewModel = new ScriptManagerDashboardViewModel
            {
                PendingPrescriptions = pendingPrescriptions,
                RecentProcessed = recentProcessed,
                PendingCount = pendingCount,
                ProcessedCount = processedCount,
            };

            return View(viewModel);
        }

        public async Task<IActionResult> StockManager()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewData["WelcomeName"] = $"{user.Title} {user.LastName}";
            }
            else
            {
                ViewData["WelcomeName"] = User.Identity?.Name ?? "User";
            }

            // Get low stock items
            var lowStockItemsDb = await _context.Consumables
                .Include(c => c.Ward)
                .Where(c => c.QuantityAvailable < 20 && !c.IsDeleted)
                .OrderBy(c => c.QuantityAvailable)
                .Take(5)
                .ToListAsync();

            // Get pending requests for display (limited to 5)
            var pendingRequestsDb = await _context.ConsumablesRequests
                .Include(cr => cr.Employee)
                .Where(cr => !cr.IsDelivered && !cr.IsDeleted)
                .OrderBy(cr => cr.RequestedDate)
                .Take(5)
                .ToListAsync();

            // Map to ViewModel
            var lowStockItems = lowStockItemsDb.Select(c => new ConsumableViewModel
            {
                ConsumableId = c.ConsumableId,
                Name = c.Name,
                QuantityAvailable = c.QuantityAvailable,
                WardName = c.Ward?.Name ?? "Unknown Ward",
                StockStatus = c.QuantityAvailable == 0 ? "Out of Stock" :
                             c.QuantityAvailable < 5 ? "Critical" : "Low",
                StatusColor = c.QuantityAvailable == 0 ? "danger" :
                             c.QuantityAvailable < 5 ? "warning" : "info",
                IsCritical = c.QuantityAvailable < 5
            }).ToList();

            var pendingRequests = pendingRequestsDb.Select(cr => new ConsumableRequestViewModel
            {
                RequestId = cr.RequestId,
                ConsumableName = cr.ConsumableName,
                QuantityRequested = cr.QuantityRequested,
                RequestedDate = cr.RequestedDate,
                RequestedBy = $"{cr.Employee?.FirstName} {cr.Employee?.LastName}",
                IsDelivered = cr.IsDelivered,
                Status = cr.IsDelivered ? "Delivered" : "Pending"
            }).ToList();

            // Calculate statistics - USE SEPARATE COUNT QUERIES
            var lowStockCount = await _context.Consumables
                .CountAsync(c => c.QuantityAvailable < 20 && !c.IsDeleted);

            var criticalStockCount = await _context.Consumables
                .CountAsync(c => c.QuantityAvailable < 5 && !c.IsDeleted);

            var outOfStockCount = await _context.Consumables
                .CountAsync(c => c.QuantityAvailable == 0 && !c.IsDeleted);

            var totalConsumables = await _context.Consumables
                .CountAsync(c => !c.IsDeleted);

            // FIX: Use separate count query for pending requests
            var pendingRequestsCount = await _context.ConsumablesRequests
                .CountAsync(cr => !cr.IsDelivered && !cr.IsDeleted);

            // Create ViewModel
            var viewModel = new StockManagerDashboardViewModel
            {
                LowStockItems = lowStockItems,
                PendingRequests = pendingRequests,
                LowStockCount = lowStockCount,
                PendingRequestsCount = pendingRequestsCount,
                TotalConsumables = totalConsumables,
                CriticalStockItems = criticalStockCount,
                OutOfStockItems = outOfStockCount
            };

            return View(viewModel);
        }
    }
}