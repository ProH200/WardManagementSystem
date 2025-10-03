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
    public class DoctorDischargeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> _userManager;

        public DoctorDischargeController(ApplicationDbContext context, UserManager<Employee> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: List patients eligible for discharge
        public async Task<IActionResult> Discharge()
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var patients = await _context.PatientAdmissions
                .Include(pa => pa.Patient)
                .Include(pa => pa.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
                .Include(pa => pa.Discharges)
                .Where(pa => pa.AssignedEmployeeId == doctor.Id &&
                           !pa.IsDeleted &&
                           !pa.Patient.IsDeleted &&
                           !pa.Discharges.Any(d => !d.IsDeleted))
                .OrderByDescending(pa => pa.AdmissionDate)
                .ToListAsync();

            return View("~/Views/Doctor/Discharge.cshtml", patients);
        }

        // GET: Discharge confirmation page
        public async Task<IActionResult> DischargePatient(int admissionId)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var admission = await _context.PatientAdmissions
                .Include(pa => pa.Patient)
                .Include(pa => pa.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
                .Include(pa => pa.AssignedEmployee)
                .FirstOrDefaultAsync(pa => pa.AdmissionId == admissionId &&
                                         pa.AssignedEmployeeId == doctor.Id &&
                                         !pa.IsDeleted);

            if (admission == null)
            {
                TempData["ToastMessage"] = "Patient admission not found.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Discharge");
            }

            var existingDischarge = await _context.Discharges
                .FirstOrDefaultAsync(d => d.AdmissionId == admissionId && !d.IsDeleted);

            if (existingDischarge != null)
            {
                TempData["ToastMessage"] = "This patient has already been discharged.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Discharge");
            }

            return View("~/Views/Doctor/DischargePatient.cshtml", admission);
        }

        // POST: Process discharge
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DischargePatient(int admissionId, string dischargeNotes)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var admission = await _context.PatientAdmissions
                .Include(pa => pa.Patient)
                .Include(pa => pa.Bed)
                .Include(pa => pa.Discharges)
                .FirstOrDefaultAsync(pa => pa.AdmissionId == admissionId &&
                                         pa.AssignedEmployeeId == doctor.Id &&
                                         !pa.IsDeleted);

            if (admission == null)
            {
                TempData["ToastMessage"] = "Patient admission not found.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Discharge");
            }

            if (admission.Discharges.Any(d => !d.IsDeleted))
            {
                TempData["ToastMessage"] = "This patient has already been discharged.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Discharge");
            }

            try
            {
                var discharge = new Discharge
                {
                    PatientId = admission.PatientId,
                    AdmissionId = admission.AdmissionId,
                    DischargeDate = DateTime.Today,
                    Description = dischargeNotes ?? "Patient discharged by doctor."
                };

                _context.Discharges.Add(discharge);

                var bed = await _context.Beds.FindAsync(admission.BedId);
                if (bed != null)
                {
                    bed.Status = "Available";
                }

                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = $"Patient {admission.Patient.FirstName} {admission.Patient.LastName} discharged successfully! Bed {bed?.BedNumber} is now available.";
                TempData["ToastType"] = "success";
                return RedirectToAction("Discharge");
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Error discharging patient: {ex.Message}";
                TempData["ToastType"] = "error";
                return RedirectToAction("DischargePatient", new { admissionId });
            }
        }

        // GET: Discharge history
        public async Task<IActionResult> DischargeHistory()
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var discharges = await _context.Discharges
                .Include(d => d.Patient)
                .Include(d => d.PatientAdmission)
                    .ThenInclude(pa => pa.Bed)
                        .ThenInclude(b => b.Room)
                            .ThenInclude(r => r.Ward)
                .Where(d => d.PatientAdmission.AssignedEmployeeId == doctor.Id && !d.IsDeleted)
                .OrderByDescending(d => d.DischargeDate)
                .ToListAsync();

            return View("~/Views/Doctor/DischargeHistory.cshtml", discharges);
        }
    }
}
