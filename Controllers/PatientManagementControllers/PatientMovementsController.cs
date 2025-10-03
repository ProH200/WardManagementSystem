using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.Controllers.PatientManagementControllers
{
    public class PatientMovementsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientMovementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Admitted()
        {
            var admittedPatients = await _context.PatientAdmissions
                .Include(a => a.Patient)
                .Include(a => a.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
                .Where(a => a.AdmissionDate != null && !a.IsDeleted)
                .ToListAsync();

            var wards = await _context.Wards
                .Include(w => w.Rooms.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.Beds.Where(b => b.Status == "Available" && !b.IsDeleted))
                .Where(w => !w.IsDeleted)
                .ToListAsync();

            ViewBag.Wards = wards;
            return View(admittedPatients);
        }

        // GET: Movements list
        public async Task<IActionResult> Movements(int admissionId)
        {
            var movements = await _context.PatientMovements
                .Include(m => m.PatientAdmission)
                    .ThenInclude(a => a.Bed)
                        .ThenInclude(b => b.Room)
                            .ThenInclude(r => r.Ward)
                .Where(m => m.AdmissionId == admissionId && !m.IsDeleted)
                .OrderByDescending(m => m.Date)
                .ToListAsync();

            ViewBag.AdmissionId = admissionId;
            return View(movements);
        }

        // GET: Edit movement form
        public async Task<IActionResult> EditMovementForm(int movementId)
        {
            var movement = await _context.PatientMovements
                .Include(m => m.PatientAdmission)
                    .ThenInclude(a => a.Patient)
                .Include(m => m.PatientAdmission)
                    .ThenInclude(a => a.Bed)
                        .ThenInclude(b => b.Room)
                            .ThenInclude(r => r.Ward)
                .FirstOrDefaultAsync(m => m.MovementId == movementId);

            if (movement == null) return NotFound();

            var wards = await _context.Wards
                .Where(w => !w.IsDeleted)
                .OrderBy(w => w.Name)
                .ToListAsync();

            ViewBag.Wards = wards;
            ViewBag.PatientName = $"{movement.PatientAdmission.Patient.FirstName} {movement.PatientAdmission.Patient.LastName}";

            return PartialView("_EditMovementForm", movement);
        }

        // GET: Delete movement form
        public async Task<IActionResult> DeleteMovementForm(int movementId)
        {
            var movement = await _context.PatientMovements
                .Include(m => m.PatientAdmission)
                    .ThenInclude(a => a.Patient)
                .FirstOrDefaultAsync(m => m.MovementId == movementId);

            if (movement == null) return NotFound();

            return PartialView("_DeleteMovementForm", movement);
        }

        // POST: Update movement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMovement(int MovementId, DateTime Date, string Reason)
        {
            if(ModelState.IsValid)
            {
                var movement = await _context.PatientMovements
                    .FirstOrDefaultAsync(m => m.MovementId == MovementId);

                if (movement == null)
                {
                    TempData["Error"] = "Movement not found";
                    return RedirectToAction("Movements", new { admissionId = movement?.AdmissionId });
                }

                movement.Date = Date;
                movement.Reason = Reason ?? string.Empty;

                if (TryValidateModel(movement))
                {
                    TempData["Error"] = "Invalid movement data. Please check all required fields.";
                    return RedirectToAction("Movements", new { admissionId = movement.AdmissionId });
                }

                _context.PatientMovements.Update(movement);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Movement updated successfully!";
                return RedirectToAction("Movements", new { admissionId = movement.AdmissionId });
            }
           
           
            TempData["Error"] = $"Error updating movement";
            return RedirectToAction("Movements");
         
        }

        // POST: Delete movement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMovement(int MovementId)
        {
            try
            {
                var movement = await _context.PatientMovements
                    .FirstOrDefaultAsync(m => m.MovementId == MovementId);

                if (movement == null)
                {
                    TempData["Error"] = "Movement not found";
                    return RedirectToAction("Movements");
                }

                movement.IsDeleted = true;
                _context.PatientMovements.Update(movement);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Movement deleted successfully!";
                return RedirectToAction("Movements", new { admissionId = movement.AdmissionId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting movement: {ex.Message}";
                return RedirectToAction("Movements");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMovement(int AdmissionId, int WardId, int RoomId, int BedId, DateTime Date, string Reason)
        {
            if(ModelState.IsValid)
            {
                var admission = await _context.PatientAdmissions
                    .Include(a => a.Bed)
                        .ThenInclude(b => b.Room)
                            .ThenInclude(r => r.Ward)
                    .FirstOrDefaultAsync(a => a.AdmissionId == AdmissionId);

                if (admission == null)
                {
                    TempData["Error"] = "Admission not found";
                    return RedirectToAction("Admitted");
                }

                var newBed = await _context.Beds
                    .Include(b => b.Room)
                        .ThenInclude(r => r.Ward)
                    .FirstOrDefaultAsync(b => b.BedId == BedId);

                if (newBed == null || newBed.Status != "Available")
                {
                    TempData["Error"] = "Selected bed is not available";
                    return RedirectToAction("Admitted");
                }

                string toLocation = $"{newBed.Room.Ward.Name} / {newBed.Room.RoomNumber} / {newBed.BedNumber}";
                string fromLocation = $"{admission.Bed?.Room?.Ward?.Name ?? "Unknown"} / {admission.Bed?.Room?.RoomNumber ?? "-"} / {admission.Bed?.BedNumber ?? "-"}";

                var movement = new PatientMovement
                {
                    AdmissionId = AdmissionId,
                    Date = Date,
                    Reason = Reason ?? string.Empty,
                    FromLocation = fromLocation,
                    ToLocation = toLocation,
                    BedId = BedId
                };

                if (admission.BedId != 0)
                {
                    var oldBed = await _context.Beds.FindAsync(admission.BedId);
                    if (oldBed != null) oldBed.Status = "Available";
                }

                admission.BedId = BedId;
                newBed.Status = "Occupied";

                _context.PatientMovements.Add(movement);
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Movement saved successfully!" });
                }

                TempData["Success"] = "Movement saved successfully!";
                return RedirectToAction("Admitted");
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Error saving movement" });
            }

            TempData["Error"] = "Error saving movement";
            return RedirectToAction("Admitted");
        }
         
    }
}