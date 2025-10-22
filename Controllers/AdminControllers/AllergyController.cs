using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.ViewModels.AdminSubsystem;

namespace Wellness_Wardens_Project.Controllers.AdminControllers
{
    public class AllergyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AllergyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Allergy
        public async Task<IActionResult> ManageAllergy()
        {
            var allergies = await _context.Allergies
                .Where(a => !a.IsDeleted)
                .Select(a => new AllergyViewModel
                {
                    AllergyId = a.AllergyId,
                    Name = a.Name,
                    Description = a.Description
                })
                .ToListAsync();

            return View(allergies);
        }

        // GET: Allergy/Create
        public IActionResult AddAllergy()
        {
            return View();
        }

        // POST: Allergy/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAllergy(AllergyViewModel model)
        {
            if (ModelState.IsValid)
            {
                var allergy = new Allergy
                {
                    Name = model.Name,
                    Description = model.Description,
                    IsDeleted = false,
                    EmployeeId = null   // Explicitly set to null
                };

                _context.Add(allergy);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Allergy created successfully!";
                return RedirectToAction(nameof(ManageAllergy));
            }
            return View(model);
        }

        // GET: Allergy/Edit/5
        public async Task<IActionResult> EditAllergy(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var allergy = await _context.Allergies
                .Where(a => a.AllergyId == id && !a.IsDeleted)
            .FirstOrDefaultAsync();

            if (allergy == null)
            {
                return NotFound();
            }

            var model = new AllergyViewModel
            {
                AllergyId = allergy.AllergyId,
                Name = allergy.Name,
                Description = allergy.Description
            };

            return View(model);
        }

        // POST: Allergy/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAllergy(int id, AllergyViewModel model)
        {
            if (id != model.AllergyId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var allergy = await _context.Allergies
                        .Where(a => a.AllergyId == id && !a.IsDeleted)
                        .FirstOrDefaultAsync();

                    if (allergy == null)
                    {
                        return NotFound();
                    }

                    allergy.Name = model.Name;
                    allergy.Description = model.Description;

                    _context.Update(allergy);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Allergy updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AllergyExists(model.AllergyId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageAllergy));
            }
            return View(model);
        }

        // POST: Allergy/DeleteAllergy/5
        [HttpPost("DeleteAllergy/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllergy(int id)
        {
            var allergy = await _context.Allergies
                .Where(a => a.AllergyId == id && !a.IsDeleted)
                .FirstOrDefaultAsync();

            if (allergy != null)
            {
                allergy.IsDeleted = true;
                _context.Update(allergy);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Allergy deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Allergy not found or already deleted.";
            }

            return RedirectToAction(nameof(ManageAllergy));
        }

        private bool AllergyExists(int id)
        {
            return _context.Allergies.Any(e => e.AllergyId == id && !e.IsDeleted);
        }
    }
}


