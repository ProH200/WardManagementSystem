using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorPrescriptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> _userManager;

        public DoctorPrescriptionController(ApplicationDbContext context, UserManager<Employee> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: List all prescriptions for the logged-in doctor
        public async Task<IActionResult> Prescriptions()
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var prescriptions = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Employee)
                .Include(p => p.PrescriptionMedications)
                    .ThenInclude(pm => pm.Medication)
                .Where(p => p.EmployeeId == doctor.Id && !p.IsDeleted)
                .OrderByDescending(p => p.DateWritten)
                .ToListAsync();

            return View("~/Views/Doctor/Prescriptions.cshtml", prescriptions);
        }

        // GET: Create a new prescription
        public async Task<IActionResult> CreatePrescription(int? patientId)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            // Get patients assigned to this doctor - SAME AS VISIT NOTES
            var patients = await _context.PatientAdmissions
                .Include(pa => pa.Patient)
                .Where(pa => pa.AssignedEmployeeId == doctor.Id &&
                           !pa.IsDeleted &&
                           !pa.Patient.IsDeleted &&
                           !pa.Discharges.Any(d => !d.IsDeleted))
                .Select(pa => pa.Patient)
                .Distinct()
                .ToListAsync();

            // Get available medications
            var medications = await _context.Medications
                .Where(m => !m.IsDeleted && m.QuantityAvailable > 0)
                .OrderBy(m => m.Name)
                .ToListAsync();

            ViewBag.Patients = patients;
            ViewBag.Medications = medications;

            var model = new Prescription
            {
                DateWritten = DateTime.Today,
                PatientId = patientId ?? 0
            };

            // ✅ SAME PATTERN AS VISIT NOTES
            return View("~/Views/Doctor/CreatePrescription.cshtml", model);
        }

        // POST: Create a new prescription
        // POST: Create a new prescription - SIMPLE VERSION LIKE VISIT NOTES
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePrescription(Prescription prescription, int[] selectedMedications, string[] dosages)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            // ✅ ALWAYS set these - NO VALIDATION CHECK
            prescription.EmployeeId = doctor.Id;
            prescription.DateWritten = DateTime.Today;

            // ✅ Add prescription medications
            if (selectedMedications != null && selectedMedications.Length > 0)
            {
                for (int i = 0; i < selectedMedications.Length; i++)
                {
                    var prescriptionMedication = new PrescriptionMedication
                    {
                        MedicationId = selectedMedications[i],
                        Dosage = dosages?[i] ?? "As directed"
                    };
                    prescription.PrescriptionMedications.Add(prescriptionMedication);
                }
            }

            // ✅ Save without validation
            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Prescription created successfully!";

            // ✅ Force redirect explicitly to the right controller - SAME PATTERN AS VISIT NOTES
            return RedirectToAction("Prescriptions", "DoctorPrescription");
        }

        // GET: View prescription details
        public async Task<IActionResult> PrescriptionDetails(int id)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Employee)
                .Include(p => p.PrescriptionMedications)
                    .ThenInclude(pm => pm.Medication)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id &&
                                        p.EmployeeId == doctor.Id &&
                                        !p.IsDeleted);

            if (prescription == null)
            {
                return NotFound();
            }

            return View("~/Views/Doctor/PrescriptionDetails.cshtml", prescription); // FIXED: Full path
        }
    }
}