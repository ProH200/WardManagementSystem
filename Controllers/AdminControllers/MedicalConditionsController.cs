using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wellness_Wardens_Project.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

[Authorize(Roles = "Admin")]
public class MedicalConditionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public MedicalConditionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET - Medical Conditions Master List
    public async Task<IActionResult> ManageConditions()
    {
        var conditions = await _context.MedicalConditions
                            .Where(mc => !mc.IsDeleted)
                            .OrderBy(mc => mc.Name)
                            .ToListAsync();
        return View(conditions);
    }

    // GET - Add Medical Condition
    [HttpGet]
    public IActionResult AddCondition()
    {
        return View();
    }

    // POST - Add Medical Condition
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCondition(MedicalCondition medicalCondition)
    {
        if (!ModelState.IsValid)
        {
            // Check for duplicate condition name
            bool exists = await _context.MedicalConditions
                .AnyAsync(mc => mc.Name == medicalCondition.Name && !mc.IsDeleted);

            if (exists)
            {
                ModelState.AddModelError("Name", "This medical condition already exists in the system.");
                return View(medicalCondition);
            }

            // Ensure PatientId is null for master list
            medicalCondition.PatientId = null;

            _context.MedicalConditions.Add(medicalCondition);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Medical condition '{medicalCondition.Name}' added successfully.";
            return RedirectToAction(nameof(ManageConditions));
        }

        return View(medicalCondition);
    }

    // GET - Edit Medical Condition
    [HttpGet]
    public async Task<IActionResult> EditCondition(int id)
    {
        var condition = await _context.MedicalConditions
                            .FirstOrDefaultAsync(mc => mc.MedicalConditionId == id && !mc.IsDeleted);

        if (condition == null)
        {
            return NotFound();
        }

        return View(condition);
    }

    // POST - Edit Medical Condition
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCondition(MedicalCondition medicalCondition)
    {
        if (!ModelState.IsValid)
        {
            bool exists = await _context.MedicalConditions
                .AnyAsync(mc => mc.Name == medicalCondition.Name &&
                               mc.MedicalConditionId != medicalCondition.MedicalConditionId &&
                               !mc.IsDeleted);

            if (exists)
            {
                ModelState.AddModelError("Name", "This medical condition already exists in the system.");
                return View(medicalCondition);
            }

            // Ensure PatientId remains null
            medicalCondition.PatientId = null;

            _context.Update(medicalCondition);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Medical condition '{medicalCondition.Name}' updated successfully.";
            return RedirectToAction(nameof(ManageConditions));
        }

        return View(medicalCondition);
    }

    // POST - Delete Medical Condition (Soft Delete)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCondition(int id)
    {
        var condition = await _context.MedicalConditions.FindAsync(id);
        if (condition == null) return NotFound();

        condition.IsDeleted = true;
        _context.Update(condition);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Medical condition '{condition.Name}' deleted successfully.";
        return RedirectToAction(nameof(ManageConditions));
    }

    // GET - Search Medical Conditions with real-time suggestions
    [HttpGet]
    public async Task<IActionResult> SearchConditions(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Json(new { success = false, message = "Please enter a search term" });
        }

        var suggestions = await _context.MedicalConditions
            .Where(mc => !mc.IsDeleted &&
                        (mc.Name.Contains(searchTerm) ||
                         mc.Description.Contains(searchTerm)))
            .Take(10)
            .Select(mc => new
            {
                id = mc.MedicalConditionId,
                name = mc.Name,
                description = mc.Description,
                displayText = mc.Name
            })
            .ToListAsync();

        return Json(new { success = true, suggestions });
    }
}