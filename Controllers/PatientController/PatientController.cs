using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;
using Wellness_Wardens_Project.Models.PatientCareSubsystem;
using Wellness_Wardens_Project.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Wellness_Wardens_Project.Models.DoctorPatientSubsystem;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;

namespace Wellness_Wardens_Project.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Nurse,Nursing Sister")]

        public async Task<IActionResult> Index()
        {
            var patients = await _context.Patients
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            return View(patients);
        }

        // ======================
        // Folder View (Patient Details + VitalSigns + Treatments + DoctorVisits)
        // ======================
        public async Task<IActionResult> Folder(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.VitalSigns)
                .Include(p => p.Treatments)
                .Include(p => p.Allergies)
                .Include(p => p.MedicalConditions)
                .Include(p => p.DoctorVisits)
                .Include(p => p.Prescriptions)
                    .ThenInclude(pr => pr.PrescriptionMedications)
                    .ThenInclude(pm => pm.Medication)
                .Include(p => p.Prescriptions)
                    .ThenInclude(pr => pr.Employee)
                .Include(p => p.PatientAdmissions)
                    .ThenInclude(pa => pa.Employee)
                .Include(p => p.PatientAdmissions)
                    .ThenInclude(pa => pa.PatientMovements)
                .FirstOrDefaultAsync(p => p.PatientId == id);

            if (patient == null) return NotFound();

            // Load VisitNotes separately
            var visitNotes = await _context.VisitNotes
                .Include(vn => vn.Doctor)
                .Where(vn => vn.PatientId == id && !vn.IsDeleted)
                .ToListAsync();

            var allMedications = await _context.Medications.Where(m => !m.IsDeleted).ToListAsync();
            var employees = await _context.Employees.ToListAsync();

            // Get all prescriptions for the patient
            var prescriptions = patient.Prescriptions?.Where(p => !p.IsDeleted).ToList() ?? new List<Prescription>();

            // Separate prescriptions by medication schedule type
            var nonScheduledPrescriptions = prescriptions
                .Where(p => p.PrescriptionMedications.Any(pm =>
                    pm.Medication.IsScheduledMedication == false))
                .ToList();

            var scheduledPrescriptions = prescriptions
                .Where(p => p.PrescriptionMedications.Any(pm =>
                    pm.Medication.IsScheduledMedication == true))
                .ToList();

            // Separate medications by schedule type
            var nonScheduledMeds = allMedications.Where(m => m.IsScheduledMedication == false).ToList();
            var scheduledMeds = allMedications.Where(m => m.IsScheduledMedication == true).ToList();

            // Populate ViewBag with medications
            ViewBag.NonScheduledMedications = nonScheduledMeds;
            ViewBag.ScheduledMedications = scheduledMeds;

            // Get admissions data
            var admissions = patient.PatientAdmissions?.Where(a => !a.IsDeleted).ToList() ?? new List<PatientAdmission>();
            var allergies = patient.Allergies.Where(a => !a.IsDeleted).ToList();
            var medicalConditions = patient.MedicalConditions.Where(a => !a.IsDeleted).ToList();

            var model = new PatientFolderViewModel
            {
                Patient = patient,
                Medications = allMedications,
                NonScheduledMedications = nonScheduledMeds,
                ScheduledMedications = scheduledMeds,
                NonScheduledPrescriptions = nonScheduledPrescriptions,
                ScheduledPrescriptions = scheduledPrescriptions,
                VisitNotes = visitNotes,  // Add the separately loaded visit notes
                VitalSigns = patient.VitalSigns.Where(v => !v.IsDeleted).ToList(),
                Treatments = patient.Treatments.Where(t => !t.IsDeleted).ToList(),
                DoctorVisits = patient.DoctorVisits.Where(d => !d.IsDeleted).ToList(),
                Allergies = allergies,
                MedicalConditions = medicalConditions,
                Prescriptions = prescriptions,
                Admissions = admissions,

                NewVitalSign = new VitalSigns { PatientId = patient.PatientId },
                NewTreatment = new Treatment { PatientId = patient.PatientId },
                NewDoctorVisit = new DoctorVisit { PatientId = patient.PatientId },
                NewPrescription = new Prescription { PatientId = patient.PatientId },
                NewAllergy = new Allergy { PatientId = patient.PatientId },
                NewMedicalCondition = new MedicalCondition { PatientId = patient.PatientId },
                NewAdmission = new PatientAdmission
                {
                    PatientId = patient.PatientId,
                    AdmissionDate = DateTime.Today
                }
            };

            ViewBag.PatientId = id;
            ViewBag.Employees = employees;
            return View(model);
        }
        // ======================
        // ====================== VITAL SIGNS ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVital(int patientId, double Tempareture, int HeartRate, string BloodPressure, DateTime recordedDate)
        {
            try
            {
                Console.WriteLine($"Received - PatientId: {patientId}, Temp: {Tempareture}, HR: {HeartRate}, BP: {BloodPressure}, Date: {recordedDate}");

                // Validate patient exists
                var patientExists = await _context.Patients.AnyAsync(p => p.PatientId == patientId);
                if (!patientExists)
                {
                    return BadRequest($"Patient with ID {patientId} does not exist.");
                }

                // Create new vital sign with the provided date
                var vital = new VitalSigns
                {
                    PatientId = patientId,
                    Tempareture = Tempareture,
                    HeartRate = HeartRate,
                    BloodPressure = BloodPressure,
                    RecordedDate = recordedDate, // Use the provided date
                    IsDeleted = false
                };

                _context.VitalSigns.Add(vital);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Vital signs added successfully.";

                return RedirectToAction("Folder", new { id = patientId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditVital(int id)
        {
            var vital = await _context.VitalSigns.FindAsync(id);
            if (vital == null) return NotFound();
            return View(vital);
        }

        [HttpPost]
        public async Task<IActionResult> EditVital(int id, double Tempareture, int HeartRate, string BloodPressure)
        {
            VitalSigns existingVital = null; // Declare outside try block

            try
            {
                // Find the existing vital sign
                existingVital = await _context.VitalSigns.FindAsync(id);
                if (existingVital == null)
                {
                    return NotFound();
                }

                // Update the properties
                existingVital.Tempareture = Tempareture;
                existingVital.HeartRate = HeartRate;
                existingVital.BloodPressure = BloodPressure;
                

                // Mark as modified and save
                _context.VitalSigns.Update(existingVital);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Vital signs updated successfully.";

                return RedirectToAction("Folder", new { id = existingVital.PatientId });
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error updating vital: {ex.Message}");

                // Use null-conditional operator to avoid null reference exception
                int patientId = existingVital?.PatientId ?? 0;

                if (patientId == 0)
                {
                    // If we don't have a patientId, redirect to index
                    return RedirectToAction("Index");
                }

                return RedirectToAction("Folder", new { id = patientId });
            }
        }

        // POST: Soft Delete Vital Sign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVital(int id)
        {
            try
            {
                var vital = await _context.VitalSigns.FindAsync(id);
                if (vital == null)
                {
                    return NotFound();
                }

                // Soft delete (set IsDeleted to true)
                vital.IsDeleted = true;
                _context.VitalSigns.Update(vital);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Vital signs deleted successfully.";

                return RedirectToAction("Folder", new { id = vital.PatientId });
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error deleting vital: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddTreatment(int patientId, string TreatmentType, DateTime DatePerformed)
        {
            if (ModelState.IsValid)
            {
                // Make sure PatientId exists
                var patientExists = await _context.Patients.AnyAsync(p => p.PatientId == patientId);
                if (!patientExists)
                {
                    ModelState.AddModelError("", "Invalid Patient. Cannot add treatment.");
                    return RedirectToAction("Folder", new { id = patientId });
                }

                var treatment = new Treatment
                {
                    PatientId = patientId,
                    TreatmentType = TreatmentType,
                    DatePerformed = DatePerformed, // This will now include the time
                    IsDeleted = false
                };

                _context.Treatments.Add(treatment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Treatment Added successfully.";
            }

            return RedirectToAction("Folder", new { id = patientId });
        }



        public async Task<IActionResult> EditTreatment(int id)
        {
            var treatment = await _context.Treatments.FindAsync(id);
            if (treatment == null) return NotFound();
            return View(treatment);
        }

        [HttpPost]
        public async Task<IActionResult> EditTreatment(int id, string TreatmentType, DateTime DatePerformed)
        {
            try
            {
                // Find the existing treatment
                var existingTreatment = await _context.Treatments.FindAsync(id);
                if (existingTreatment == null)
                {
                    return NotFound();
                }

                // Update the properties
                existingTreatment.TreatmentType = TreatmentType;
                existingTreatment.DatePerformed = DatePerformed;

                // Mark as modified and save
                _context.Treatments.Update(existingTreatment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Treatment updated successfully.";

                return RedirectToAction("Folder", new { id = existingTreatment.PatientId });
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error updating treatment: {ex.Message}");

                // Try to get the patientId from the database
                var treatment = await _context.Treatments.FindAsync(id);
                if (treatment != null)
                {
                    return RedirectToAction("Folder", new { id = treatment.PatientId });
                }

                return RedirectToAction("Index");
            }
        }

        // POST: Soft Delete Treatment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTreatment(int id)
        {
            try
            {
                var treatment = await _context.Treatments.FindAsync(id);
                if (treatment == null)
                {
                    return NotFound();
                }

                // Soft delete (set IsDeleted to true)
                treatment.IsDeleted = true;
                _context.Treatments.Update(treatment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Treatment deleted successfully.";

                return RedirectToAction("Folder", new { id = treatment.PatientId });
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error deleting treatment: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        // GET: Display the form to add a new DoctorVisit
        [HttpGet]
        public IActionResult AddDoctorVisit(int patientId)
        {
            var patient = _context.Patients.Find(patientId);
            if (patient == null) return NotFound();

            var model = new DoctorVisit
            {
                PatientId = patientId,
                VisitDate = DateTime.Today // Default to today's date
            };

            return View(model);
        }

        // POST: Add a new DoctorVisit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctorVisit(DoctorVisit doctorVisit)
        {
            if (ModelState.IsValid)
            {
                // Verify patient exists
                var patientExists = await _context.Patients.AnyAsync(p => p.PatientId == doctorVisit.PatientId);
                if (!patientExists)
                {
                    ModelState.AddModelError("", "Invalid patient selected.");
                    return View(doctorVisit);
                }

                doctorVisit.IsDeleted = false;
                _context.DoctorVisits.Add(doctorVisit);
                await _context.SaveChangesAsync();

                // Redirect back to the patient folder to see the updated table
                return RedirectToAction("Folder", new { id = doctorVisit.PatientId });
            }

            return View(doctorVisit);
        }

        [HttpGet]
        public IActionResult AddPrescription(int patientId)
        {
            var patient = _context.Patients.Find(patientId);
            if (patient == null) return NotFound();

            // Get medications for dropdowns
            ViewBag.NonScheduledMedications = _context.Medications
                .Where(m => !m.IsScheduledMedication && !m.IsDeleted)
                .ToList();

            ViewBag.ScheduledMedications = _context.Medications
                .Where(m => m.IsScheduledMedication && !m.IsDeleted)
                .ToList();

            var model = new Prescription
            {
                PatientId = patientId,
                DateWritten = DateTime.Today,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPrescription(int patientId, int medicationId, string medicationType,
 string dosage, string frequency, int durationDays, string instructions = "")
        {
            try
            {
                // Get current user's role
                var currentUserRole = User.IsInRole("Nursing Sister") ? "Nursing Sister" :
                                     User.IsInRole("Nurse") ? "Nurse" :
                                     User.IsInRole("Doctor") ? "Doctor" : "Unknown";

                // Get the medication to check if it's scheduled
                var medication = await _context.Medications.FindAsync(medicationId);
                if (medication == null)
                {
                    return BadRequest("Medication not found.");
                }

                // Access control logic based on IsScheduledMedication only
                if (medication.IsScheduledMedication == false)
                {
                    // Only Nurses can administer non-scheduled medications
                    if (!User.IsInRole("Nurse"))
                    {
                        return Forbid("Only nurses can administer non-scheduled medications.");
                    }
                }
                else // IsScheduledMedication == true
                {
                    // Only Nursing Sisters and Doctors can administer scheduled medications
                    if (!User.IsInRole("Nursing Sister") && !User.IsInRole("Doctor"))
                    {
                        return Forbid("Only nursing sisters and doctors can administer scheduled medications.");
                    }
                }

                // Get current user (employee) ID
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var prescription = new Prescription
                {
                    PatientId = patientId,
                    DateWritten = DateTime.Today,
                    Instructions = instructions,
                    EmployeeId = currentUserId, // Use current logged-in user
                    IsDeleted = false
                };

                var prescriptionMedication = new PrescriptionMedication
                {
                    MedicationId = medicationId,
                    Dosage = dosage,
                    Frequency = frequency,
                    DurationDays = durationDays,
                    Prescription = prescription
                };

                _context.Prescriptions.Add(prescription);
                prescription.PrescriptionMedications = new List<PrescriptionMedication> { prescriptionMedication };

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Medication added successfully.";

                return RedirectToAction("Folder", new { id = patientId });
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error saving prescription: {ex.Message}");
                return RedirectToAction("Folder", new { id = patientId });
            }
        }
        // Edit Prescription Medication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPrescription(int prescriptionId, int medicationId, string dosage, string frequency, int durationDays, string instructions)
        {
                var prescriptionMedication = await _context.PrescriptionMedications
                    .FirstOrDefaultAsync(pm => pm.PrescriptionId == prescriptionId && pm.MedicationId == medicationId);

                if (prescriptionMedication == null)
                {
                    return NotFound();
                }

                prescriptionMedication.Dosage = dosage;
                prescriptionMedication.Frequency = frequency;
                prescriptionMedication.DurationDays = durationDays;

                var prescription = await _context.Prescriptions.FindAsync(prescriptionId);
                if (prescription != null)
                {
                    prescription.Instructions = instructions;
                }

                await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Prescription updated successfully.";
            return RedirectToAction("Folder", new { id = prescription.PatientId });
            
            
        }

        // Delete Prescription Medication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePrescription(int prescriptionId, int medicationId)
        {
            try
            {
                var prescriptionMedication = await _context.PrescriptionMedications
                    .Include(pm => pm.Prescription)
                    .FirstOrDefaultAsync(pm => pm.PrescriptionId == prescriptionId && pm.MedicationId == medicationId);

                if (prescriptionMedication == null)
                {
                    return NotFound();
                }

                // Get patient ID before deleting
                var patientId = prescriptionMedication.Prescription.PatientId;

                _context.PrescriptionMedications.Remove(prescriptionMedication);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Medication deleted successfully.";

                return RedirectToAction("Folder", new { id = patientId });
            }
            catch (Exception ex)
            {
                // Handle error - try to get patient ID from the prescription ID
                var prescription = await _context.Prescriptions.FindAsync(prescriptionId);
                if (prescription != null)
                {
                    return RedirectToAction("Folder", new { id = prescription.PatientId });
                }
                return RedirectToAction("Index");
            }
        }
    }
}
