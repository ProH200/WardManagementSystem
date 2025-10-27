using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorVisitController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> _userManager;

        public DoctorVisitController(ApplicationDbContext context, UserManager<Employee> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: List all visit notes for the logged-in doctor
        public async Task<IActionResult> RecordVisits()
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var visitNotes = await _context.VisitNotes
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Where(v => v.DoctorId == doctor.Id && !v.IsDeleted)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync();

            return View("~/Views/Doctor/RecordVisits.cshtml", visitNotes);
        }

        // GET: Create a new visit note
        public async Task<IActionResult> CreateVisitNote(int? patientId)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            // Get patients assigned to this doctor
            var patients = await _context.PatientAdmissions
                .Include(pa => pa.Patient)
                .Where(pa => pa.AssignedEmployeeId == doctor.Id &&
                           !pa.IsDeleted &&
                           !pa.Patient.IsDeleted &&
                           !pa.Discharges.Any(d => !d.IsDeleted))
                .Select(pa => pa.Patient)
                .Distinct()
                .ToListAsync();

            ViewBag.Patients = patients;

            // If patientId is provided, pre-select that patient
            var model = new VisitNote
            {
                VisitDate = DateTime.Now,
                PatientId = patientId ?? 0
            };

            return View("~/Views/Doctor/CreateVisitNote.cshtml", model);
        }

        // POST: Create a new visit note
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVisitNote(VisitNote visitNote)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            // ✅ Always set these
            visitNote.DoctorId = doctor.Id;
            visitNote.CreatedAt = DateTime.Now;

            // ✅ Save without checking ModelState for now
            _context.VisitNotes.Add(visitNote);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Visit note created successfully!";

            // ✅ Force redirect explicitly to the right controller
            return RedirectToAction("RecordVisits", "DoctorVisit");
        }

        // GET: View a specific visit note
        public async Task<IActionResult> VisitNoteDetails(int id)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var visitNote = await _context.VisitNotes
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .FirstOrDefaultAsync(v => v.VisitNoteId == id &&
                                        v.DoctorId == doctor.Id &&
                                        !v.IsDeleted);

            if (visitNote == null)
            {
                return NotFound();
            }

            return View("~/Views/Doctor/VisitNoteDetails.cshtml", visitNote);
        }

        // GET: Delete visit note confirmation
        public async Task<IActionResult> DeleteVisitNoteConfirmation(int id)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var visitNote = await _context.VisitNotes
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .FirstOrDefaultAsync(v => v.VisitNoteId == id &&
                                        v.DoctorId == doctor.Id &&
                                        !v.IsDeleted);

            if (visitNote == null)
            {
                TempData["ErrorMessage"] = "Visit note not found or you don't have permission to delete it.";
                return RedirectToAction("RecordVisits", "DoctorVisit");
            }

            return View("~/Views/Doctor/DeleteVisitNoteConfirmation.cshtml", visitNote);
        }     

        // POST: Delete visit note
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVisitNote(int id)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Unauthorized access.";
                return RedirectToAction("RecordVisits", "DoctorVisit");
            }

            var visitNote = await _context.VisitNotes
                .Include(v => v.Patient)
                .FirstOrDefaultAsync(v => v.VisitNoteId == id &&
                                        v.DoctorId == doctor.Id &&
                                        !v.IsDeleted);

            if (visitNote == null)
            {
                TempData["ErrorMessage"] = "Visit note not found or you don't have permission to delete it.";
                return RedirectToAction("RecordVisits", "DoctorVisit");
            }

            try
            {
                // Soft delete
                visitNote.IsDeleted = true;
                visitNote.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Visit note for {visitNote.Patient?.FirstName} {visitNote.Patient?.LastName} deleted successfully!";
                return RedirectToAction("RecordVisits", "DoctorVisit");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting visit note: {ex.Message}";
                return RedirectToAction("RecordVisits", "DoctorVisit");
            }
        }

        private bool VisitNoteExists(int id)
        {
            return _context.VisitNotes.Any(e => e.VisitNoteId == id && !e.IsDeleted);
        }
    }
}