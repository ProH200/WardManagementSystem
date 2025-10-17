using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.Controllers.AdminControllers
{
    public class Cons_MedController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> _userManager;

        public Cons_MedController(ApplicationDbContext context, UserManager<Employee> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /*========================
          Consumables Actions
        =========================*/

        // GET: Manage Consumables
        [HttpGet]
        public async Task<IActionResult> ManageConsumables()
        {
            var consumables = await _context.Consumables
                .Where(c => !c.IsDeleted)
                .Include(c => c.Ward)
                .Include(c => c.Employee)
                .Include(c => c.ConsumablesRequest)
                .ToListAsync();

            ViewBag.Wards = await _context.Wards
                            .Where(w => !w.IsDeleted)
                            .ToListAsync();

            return View(consumables);
        }

        // GET: Add Consumable
        [HttpGet]
        public async Task<IActionResult> AddConsumable()
        {
            ViewBag.Wards = await _context.Wards
                                    .Where(w => !w.IsDeleted)
                                    .ToListAsync();

            ViewBag.Employees = await _context.Employees
                                    .Where(e => !e.IsDeleted)
                                    .ToListAsync();

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddConsumable(Consumable consumable)
        {
            ModelState.Remove("Ward");
            ModelState.Remove("Employee");
            ModelState.Remove("ConsumablesRequest");

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == user.Id && !e.IsDeleted);

                if (employee == null)
                {
                    return Unauthorized();
                }

                consumable.EmployeeId = employee.Id;

                _context.Add(consumable);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{consumable.Name}' added successfully.";
                return RedirectToAction("ManageConsumables");
            }

            return RedirectToAction("ManageConsumables", consumable);
        }

        // GET: Edit Consumable
        [HttpGet]
        public async Task<IActionResult> EditConsumable(int id)
        {
            var consumable = await _context.Consumables
                .FirstOrDefaultAsync(c => c.ConsumableId == id && !c.IsDeleted);

            if (consumable == null) return NotFound();

            // Populate dropdowns
            ViewBag.Wards = await _context.Wards
                .Where(w => !w.IsDeleted)
                .ToListAsync();

            ViewBag.Employees = await _context.Employees
                .Where(e => !e.IsDeleted)
                .ToListAsync();

            return View(consumable);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConsumable(Consumable consumable)
        {
            ModelState.Remove("Ward");
            ModelState.Remove("Employee");
            ModelState.Remove("ConsumablesRequest");

            if (ModelState.IsValid)
            {
                var employee = await _userManager.GetUserAsync(User);

                if (employee == null)
                {
                    return Unauthorized();
                }

                consumable.EmployeeId = employee.Id;

                _context.Update(consumable);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{consumable.Name}'updated successfully.";
                return RedirectToAction(nameof(ManageConsumables));
            }

            ViewBag.Wards = await _context.Wards.Where(w => !w.IsDeleted).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => !e.IsDeleted).ToListAsync();
            TempData["ErrorMessage"] = "Failed to update medication. Please check the form.";
            return RedirectToAction(nameof(ManageConsumables));
            
        }

        // POST: Perform Soft Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeleteConsumable(int id)
        {
            var consumable = await _context.Consumables.FindAsync(id);
            if (consumable == null)
            {
                TempData["ErrorMessage"] = "Consumable not found.";
                return RedirectToAction("ManageConsumables");
            }

            consumable.IsDeleted = true;
            _context.Update(consumable);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Consumable deleted successfully.";
            return RedirectToAction("ManageConsumables");
        }

        /*========================
          Medication Actions
        =========================*/

        // GET: Manage Medications
        [HttpGet]
        public async Task<IActionResult> ManageMedication()
        {
            var meds = await _context.Medications
                .Include(m => m.Employee)
                .Include(m => m.PrescriptionMedications)
                .Where(m => !m.IsDeleted)
                .ToListAsync();

            return View(meds);
        }

        // GET: Add Medication
        [HttpGet]
        public IActionResult AddMedication()
        {
            return View();
        }

        // POST: Add Medication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedication(Medication medication)
        {
            ModelState.Remove("Employee");

            if (ModelState.IsValid)
            {
                var employee = await _userManager.GetUserAsync(User);

                if (employee == null)
                {
                    return Unauthorized();
                }

                medication.EmployeeId = employee.Id;

                _context.Add(medication);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{medication.Name} added successfully!";
                return RedirectToAction("ManageMedication");
            }

            TempData["ErrorMessage"] = "Failed to add medication. Please check the form.";
            return RedirectToAction("ManageMedication");
        }

        // GET: Edit Medication
        [HttpGet]
        public async Task<IActionResult> EditMedication(int id)
        {
            var medication = await _context.Medications.FindAsync(id);
            if (medication == null) return NotFound();

            return View(medication);
        }


        // POST: Edit Medication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMedication(Medication medication)
        {
            ModelState.Remove("Employee");

            if (ModelState.IsValid)
            {
                var employee = await _userManager.GetUserAsync(User);
                if (employee == null)
                {
                    return Unauthorized();
                }

                medication.EmployeeId = employee.Id;

                _context.Update(medication);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{medication.Name} updated successfully!";
                return RedirectToAction("ManageMedication");
            }
            TempData["ErrorMessage"] = "Failed to update medication. Please check the form.";
            return RedirectToAction("ManageMedication");
        }


        // POST: Soft Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeleteMedication(int id)
        {
            var medication = await _context.Medications.FindAsync(id);
            if (medication == null) return NotFound();

            medication.IsDeleted = true;
            _context.Update(medication);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{medication.Name} deleted successfully!";
            return RedirectToAction("ManageMedication");
        }
    }
}

