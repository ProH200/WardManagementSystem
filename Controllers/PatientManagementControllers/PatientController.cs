using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;
using Wellness_Wardens_Project.ViewModels;
using Wellness_Wardens_Project.ViewModels.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Controllers.PatientManagementControllers
{
    [Authorize(Roles = "Ward Admin")]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> _userManager;

        public PatientController(ApplicationDbContext context, UserManager<Employee> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /*=============================
          Patient Actions
          ===========================*/
        //GET:
        [HttpGet]
        public IActionResult ManagePatients()
        {
            var patients = _context.Patients
                .Where(p => !p.IsDeleted)
                .Include(p => p.PatientAdmissions)
                    .ThenInclude(pa => pa.Bed)
                        .ThenInclude(b => b.Room)
                            .ThenInclude(r => r.Ward)
                .Include(p => p.PatientAdmissions)
                    .ThenInclude(pa => pa.AssignedEmployee)
                .Include(p => p.MedicalHistories) // keep your existing include
                .Select(p => new PatientViewModel
                {
                    PatientId = p.PatientId,
                    FullName = p.FirstName + " " + p.LastName,
                    Gender = p.Gender,
                    DateOfBirth = p.DateOfBirth,
                    IdentityNumber = p.IdentityNumber,
                    PhoneNumber = p.PhoneNumber,
                    EmergencyContact = p.EmergencyContact,
                    Email = p.Email,
                    HomeAddress = p.HomeAddress,
                    ChronicCondition = p.ChronicCondition,
                    ChronicMedication = p.ChronicMedication,

                    // Add these computed properties
                    CurrentLocation = p.PatientAdmissions
                        .Where(pa => !pa.Discharges.Any())
                        .Select(pa => $"{pa.Bed.Room.Ward.Name} - {pa.Bed.Room.RoomNumber} - {pa.Bed.BedNumber}")
                        .FirstOrDefault() ?? "N/A",

                    AssignedEmployee = p.PatientAdmissions
                        .Where(pa => !pa.Discharges.Any() && pa.AssignedEmployee != null)
                        .Select(pa => pa.AssignedEmployee.Title + " " + pa.AssignedEmployee.LastName)
                        .FirstOrDefault() ?? "N/A"
                })
                .ToList();

            return View(patients);
        }


        [HttpGet]
        public IActionResult AddPatient()
        {
            ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
            return View("PatientForm", new Patient());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPatient(Patient patient)
        {
            if (!ModelState.IsValid)
            {
                // ✅ Check if patient already exists (using IdentityNumber as unique identifier)
                var existingPatient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.IdentityNumber == patient.IdentityNumber);

                if (existingPatient != null)
                {
                    ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
                    TempData["ErrorMessage"] = "A patient with this Identity Number already exists.";
                    return View("PatientForm", patient);
                }

                var employee = await _userManager.GetUserAsync(User);
                if (employee != null)
                {
                    patient.EmployeeId = employee.Id;
                }

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{patient.FirstName} added successfully.";
                return RedirectToAction("ManagePatients");
            }
            ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
            return View("PatientForm", patient);

        }

        // GET: Edit Patient
        [HttpGet]
        public async Task<IActionResult> EditPatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
            return View("PatientForm", patient); // same form as Add
        }

        // POST: Edit Patient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPatient(Patient patient)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
                TempData["ErrorMessage"] = "Failed to update patient.";
                return View("PatientForm");
            }

            var employee = await _userManager.GetUserAsync(User);
            if (employee != null)
            {
                patient.EmployeeId = employee.Id;
            }

            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{patient.FirstName} updated successfully.";
            return RedirectToAction("ManagePatients");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeletePatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            // Soft delete implementation
            patient.IsDeleted = true;
            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();

            // Success message
            TempData["SuccessMessage"] = $"Patient {patient.FirstName} has been deleted successfully.";

            return RedirectToAction("ManagePatients"); // ✅ redirect to patients, not employees
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            var history = await _context.MedicalHistories
                .Where(h => h.PatientId == patientId && !h.IsDeleted)
                .OrderByDescending(h => h.DateAdded)
                .ToListAsync();

            ViewBag.Patient = patient;

            return View(history);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHistory(int PatientId, string Notes)
        {
            if (string.IsNullOrWhiteSpace(Notes))
            {
                ModelState.AddModelError("Notes", "Notes are required.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid input. Please check your data.";
                return RedirectToAction("Details", new { id = PatientId });
            }

            var history = new PatientMedicalHistory
            {
                PatientId = PatientId,
                Notes = Notes,
                DateAdded = DateTime.Now,
                EmployeeId = User.Identity?.Name // ✅ track logged-in employee
            };

            _context.MedicalHistories.Add(history);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Medical history added successfully!";
            return RedirectToAction(nameof(GetHistory), new { PatientId });
        }

        // POST: Edit History
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHistory(PatientMedicalHistory model)
        {
            if (!ModelState.IsValid)
            {
                var history = await _context.MedicalHistories.FindAsync(model.Id);
                if (history != null)
                {
                    history.Notes = model.Notes;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Medical history updated successfully!";
                }
                return RedirectToAction(nameof(GetHistory), new { patientId = model.PatientId });
            }

            TempData["ErrorMessage"] = "Failed to update medical history.";
            return RedirectToAction(nameof(GetHistory), new { patientId = model.PatientId });
        }

        // POST: Delete History
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHistory(int Id, int patientId)
        {
            var history = await _context.MedicalHistories.FindAsync(Id);
            if (history != null)
            {
                history.IsDeleted = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Medical history deleted successfully!";
            }

            return RedirectToAction(nameof(GetHistory), new { patientId });
        }
    }
}
