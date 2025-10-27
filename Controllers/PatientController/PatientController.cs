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
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Identity;

namespace Wellness_Wardens_Project.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> _userManager;

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
                .Include(p => p.PatientAllergies)
                    .ThenInclude(pa => pa.Allergy)
                .Include(p => p.PatientMedicalConditions)
                    .ThenInclude(pmc => pmc.MedicalCondition)
                .Include(p => p.DoctorVisits)
                .Include(p => p.Prescriptions)
                    .ThenInclude(pr => pr.PrescriptionMedications)
                    .ThenInclude(pm => pm.Medication)
                .Include(p => p.Prescriptions)
                    .ThenInclude(pr => pr.Employee)
                // Add includes for medication assignments
                .Include(p => p.PatientMedications)
                    .ThenInclude(pm => pm.Medication)
                .Include(p => p.PatientMedications)
                    .ThenInclude(pm => pm.Employee)
                .Include(p => p.PatientAdmissions)
                    .ThenInclude(pa => pa.Employee)
                .Include(p => p.PatientAdmissions)
                    .ThenInclude(pa => pa.PatientMovements)
                .Include(p => p.PatientAdmissions)
                    .ThenInclude(pa => pa.Discharges)
                .Include(p => p.PatientAdmissions)
                    .ThenInclude(pa => pa.Bed)
                        .ThenInclude(b => b.Room)
                            .ThenInclude(r => r.Ward)
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
            var allPrescriptions = patient.Prescriptions?.Where(p => !p.IsDeleted).ToList() ?? new List<Prescription>();

            // Filter prescriptions based on user role
            List<Prescription> filteredPrescriptions;
            Prescription recentPrescription;

            if (User.IsInRole("Nurse"))
            {
                // Nurse sees only prescriptions with NO scheduled medications
                filteredPrescriptions = allPrescriptions
                    .Where(p => !p.PrescriptionMedications.Any(pm =>
                        pm.Medication.IsScheduledMedication == true)) // Exclude if ANY medication is scheduled
                    .ToList();

                recentPrescription = filteredPrescriptions
                    .OrderByDescending(p => p.DateWritten)
                    .FirstOrDefault();
            }
            else if (User.IsInRole("Nursing Sister"))
            {
                // Nursing Sister sees only prescriptions WITH scheduled medications
                filteredPrescriptions = allPrescriptions
                    .Where(p => p.PrescriptionMedications.Any(pm =>
                        pm.Medication.IsScheduledMedication == true)) // Include if ANY medication is scheduled
                    .ToList();

                recentPrescription = filteredPrescriptions
                    .OrderByDescending(p => p.DateWritten)
                    .FirstOrDefault();
            }
            else
            {
                // Other roles (Doctor, Admin, etc.) see all prescriptions
                filteredPrescriptions = allPrescriptions;
                recentPrescription = allPrescriptions
                    .OrderByDescending(p => p.DateWritten)
                    .FirstOrDefault();
            }

            // Separate prescriptions by medication schedule type (for other parts of the view)
            var nonScheduledPrescriptions = allPrescriptions
                .Where(p => p.PrescriptionMedications.All(pm =>
                    pm.Medication.IsScheduledMedication == false)) // ALL medications are non-scheduled
                .ToList();

            var scheduledPrescriptions = allPrescriptions
                .Where(p => p.PrescriptionMedications.Any(pm =>
                    pm.Medication.IsScheduledMedication == true)) // ANY medication is scheduled
                .ToList();

            // Get active medication assignments (many-to-many)
            var activeMedicationAssignments = patient.PatientMedications?
                .Where(pm => !pm.IsDeleted && pm.IsActive)
                .ToList() ?? new List<PatientMedication>();

            // Separate assignments by schedule type
            var nonScheduledAssignments = activeMedicationAssignments
                .Where(pm => !pm.Medication.IsScheduledMedication)
                .ToList();

            var scheduledAssignments = activeMedicationAssignments
                .Where(pm => pm.Medication.IsScheduledMedication)
                .ToList();

            // Separate medications by schedule type
            var nonScheduledMeds = allMedications.Where(m => m.IsScheduledMedication == false).ToList();
            var scheduledMeds = allMedications.Where(m => m.IsScheduledMedication == true).ToList();

            // Populate ViewBag with medications based on role
            if (User.IsInRole("Nurse"))
            {
                ViewBag.NonScheduledMedications = nonScheduledMeds;
                ViewBag.ScheduledMedications = new List<Medication>(); // Empty for nurses
            }
            else if (User.IsInRole("Nursing Sister"))
            {
                ViewBag.NonScheduledMedications = new List<Medication>(); // Empty for nursing sisters
                ViewBag.ScheduledMedications = scheduledMeds;
            }
            else
            {
                // Other roles see all medications
                ViewBag.NonScheduledMedications = nonScheduledMeds;
                ViewBag.ScheduledMedications = scheduledMeds;
            }

            // Get admissions data
            var admissions = patient.PatientAdmissions?.Where(a => !a.IsDeleted).ToList() ?? new List<PatientAdmission>();

            // Find current admission (not discharged) - using Discharges navigation property
            var currentAdmission = admissions
                .Where(a => !a.Discharges.Any(d => !d.IsDeleted))
                .OrderByDescending(a => a.AdmissionDate)
                .FirstOrDefault();

            // Get allergies and medical conditions from junction tables
            var allergies = patient.PatientAllergies?
                .Where(pa => !pa.IsDeleted)
                .Select(pa => pa.Allergy)
                .ToList() ?? new List<Allergy>();

            var medicalConditions = patient.PatientMedicalConditions?
                .Where(pmc => !pmc.IsDeleted)
                .Select(pmc => pmc.MedicalCondition)
                .ToList() ?? new List<MedicalCondition>();

            var model = new PatientFolderViewModel
            {
                Patient = patient,
                Medications = allMedications,
                ScheduledMedications = scheduledMeds,
                // Add the new assignment collections
                NonScheduledAssignments = nonScheduledAssignments,
                ScheduledAssignments = scheduledAssignments,
                VisitNotes = visitNotes,
                VitalSigns = patient.VitalSigns.Where(v => !v.IsDeleted).ToList(),
                Treatments = patient.Treatments.Where(t => !t.IsDeleted).ToList(),
                DoctorVisits = patient.DoctorVisits.Where(d => !d.IsDeleted).ToList(),
                Allergies = allergies,
                MedicalConditions = medicalConditions,
                Prescriptions = filteredPrescriptions, // Use filtered prescriptions based on role
                RecentPrescription = recentPrescription,
                HasPrescriptions = filteredPrescriptions.Any(), // Check filtered prescriptions
                Admissions = admissions,
                CurrentAdmission = currentAdmission,
                IsCurrentlyAdmitted = currentAdmission != null,

                NewVitalSign = new VitalSigns { PatientId = patient.PatientId },
                NewTreatment = new Treatment { PatientId = patient.PatientId },
                NewDoctorVisit = new DoctorVisit { PatientId = patient.PatientId },
                NewPrescription = new Prescription { PatientId = patient.PatientId },
                // Add new assignment form model
                NewMedicationAssignment = new PatientMedication { PatientId = patient.PatientId },
                NewAdmission = new PatientAdmission
                {
                    PatientId = patient.PatientId,
                    AdmissionDate = DateTime.Today
                }
            };

            ViewBag.PatientId = id;
            ViewBag.Employees = employees;
            ViewBag.UserRole = User.IsInRole("Nurse") ? "Nurse" :
                              User.IsInRole("Nursing Sister") ? "NursingSister" : "Other";

            // Add current location to ViewBag for easy access in the view
            if (currentAdmission != null && currentAdmission.Bed != null)
            {
                ViewBag.CurrentWard = currentAdmission.Bed.Room?.Ward?.Name;
                ViewBag.CurrentRoom = currentAdmission.Bed.Room?.RoomNumber;
                ViewBag.CurrentBed = currentAdmission.Bed?.BedNumber;
                ViewBag.CurrentLocation = $"{currentAdmission.Bed.Room?.Ward?.Name} - Room {currentAdmission.Bed.Room?.RoomNumber} - Bed {currentAdmission.Bed?.BedNumber}";
            }
            else
            {
                ViewBag.CurrentWard = "Not Admitted";
                ViewBag.CurrentRoom = "N/A";
                ViewBag.CurrentBed = "N/A";
                ViewBag.CurrentLocation = "Not Currently Admitted";
            }

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
        public IActionResult AssignMedication(int patientId)
        {
            var patient = _context.Patients
                .FirstOrDefault(p => p.PatientId == patientId);

            if (patient == null) return NotFound();

            // Get current user's role
            var currentUserRole = User.IsInRole("Nursing Sister") ? "Nursing Sister" :
                                 User.IsInRole("Nurse") ? "Nurse" :
                                 User.IsInRole("Doctor") ? "Doctor" : "Unknown";

            // Get medications based on current user's role
            List<Medication> medications;

            if (currentUserRole == "Nurse")
            {
                // Nurses can only see non-scheduled medications
                medications = _context.Medications
                    .Where(m => !m.IsScheduledMedication && !m.IsDeleted && m.QuantityAvailable > 0)
                    .ToList();
            }
            else if (currentUserRole == "Nursing Sister" || currentUserRole == "Doctor")
            {
                // Nursing Sisters and Doctors can see all medications
                medications = _context.Medications
                    .Where(m => !m.IsDeleted && m.QuantityAvailable > 0)
                    .ToList();
            }
            else
            {
                medications = new List<Medication>();
            }

            ViewBag.Medications = medications;
            ViewBag.CurrentUserRole = currentUserRole;

            return View(new PatientFolderViewModel
            {
                PatientId = patientId,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                AssignmentDate = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignMedication(PatientFolderViewModel model)
        {
            try
            {
                // Validate patient exists
                var patient = await _context.Patients.FindAsync(model.PatientId);
                if (patient == null)
                {
                    return NotFound("Patient not found.");
                }

                // Get the medication from existing data
                var medication = await _context.Medications.FindAsync(model.MedicationId);
                if (medication == null)
                {
                    TempData["ErrorMessage"] = "Medication not found.";
                    return RedirectToAction("AssignMedication", new { patientId = model.PatientId });
                }

                // Check if sufficient quantity is available
                if (medication.QuantityAvailable < model.QuantityAssigned)
                {
                    TempData["ErrorMessage"] = $"Insufficient quantity available. Only {medication.QuantityAvailable} units in stock.";
                    return RedirectToAction("AssignMedication", new { patientId = model.PatientId });
                }

                // Get current user's role
                var currentUserRole = User.IsInRole("Nursing Sister") ? "Nursing Sister" :
                                     User.IsInRole("Nurse") ? "Nurse" :
                                     User.IsInRole("Doctor") ? "Doctor" : "Unknown";

                // Role-based access control
                if (medication.IsScheduledMedication)
                {
                    if (!User.IsInRole("Nursing Sister") && !User.IsInRole("Doctor"))
                    {
                        return Forbid("Only nursing sisters and doctors can assign scheduled medications.");
                    }
                }
                else
                {
                    if (!User.IsInRole("Nurse"))
                    {
                        return Forbid("Only nurses can assign non-scheduled medications.");
                    }
                }

                // Get current user (employee) ID
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Create direct medication assignment using PatientMedication (many-to-many)
                var patientMedication = new PatientMedication
                {
                    PatientId = model.PatientId,
                    MedicationId = model.MedicationId,
                    EmployeeId = currentUserId,
                    AssignmentDate = DateTime.Now,
                    Dosage = model.Dosage,
                    Frequency = model.Frequency,
                    QuantityAssigned = model.QuantityAssigned,
                    DurationDays = model.DurationDays,
                    AdministrationNotes = model.AdministrationNotes,
                    IsActive = true,
                    IsDeleted = false
                };

                // Update medication inventory - reduce available quantity
                medication.QuantityAvailable -= model.QuantityAssigned;

                _context.PatientMedications.Add(patientMedication);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{medication.Name} assigned successfully to {patient.FirstName} {patient.LastName}. Quantity updated in inventory.";
                return RedirectToAction("Folder", new { id = model.PatientId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error assigning medication: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while assigning medication.";
                return RedirectToAction("AssignMedication", new { patientId = model.PatientId });
            }
        }

        // Edit Assigned Medication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssignedMedication(int patientMedicationId, string dosage, string frequency, int durationDays, string administrationNotes)
        {
            try
            {
                var patientMedication = await _context.PatientMedications
                    .Include(pm => pm.Patient)
                    .Include(pm => pm.Medication)
                    .FirstOrDefaultAsync(pm => pm.PatientMedicationId == patientMedicationId);

                if (patientMedication == null)
                {
                    return NotFound("Medication assignment not found.");
                }

                // Get current user's role for authorization check
                var currentUserRole = User.IsInRole("Nursing Sister") ? "Nursing Sister" :
                                     User.IsInRole("Nurse") ? "Nurse" :
                                     User.IsInRole("Doctor") ? "Doctor" : "Unknown";

                // Role-based access control
                if (patientMedication.Medication.IsScheduledMedication)
                {
                    if (!User.IsInRole("Nursing Sister") && !User.IsInRole("Doctor"))
                    {
                        return Forbid("Only nursing sisters and doctors can edit scheduled medications.");
                    }
                }
                else
                {
                    if (!User.IsInRole("Nurse"))
                    {
                        return Forbid("Only nurses can edit non-scheduled medications.");
                    }
                }

                // Update the medication assignment
                patientMedication.Dosage = dosage;
                patientMedication.Frequency = frequency;
                patientMedication.DurationDays = durationDays;
                patientMedication.AdministrationNotes = administrationNotes;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Medication assignment updated successfully.";
                return RedirectToAction("Folder", new { id = patientMedication.PatientId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error editing medication assignment: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while updating the medication assignment.";
                return RedirectToAction("Folder", new { id = await GetPatientIdFromAssignment(patientMedicationId) });
            }
        }

        // Soft Delete Assigned Medication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeleteAssignedMedication(int patientMedicationId)
        {
            try
            {
                var patientMedication = await _context.PatientMedications
                    .Include(pm => pm.Patient)
                    .Include(pm => pm.Medication)
                    .FirstOrDefaultAsync(pm => pm.PatientMedicationId == patientMedicationId);

                if (patientMedication == null)
                {
                    return NotFound("Medication assignment not found.");
                }

                // Get current user's role for authorization check
                var currentUserRole = User.IsInRole("Nursing Sister") ? "Nursing Sister" :
                                     User.IsInRole("Nurse") ? "Nurse" :
                                     User.IsInRole("Doctor") ? "Doctor" : "Unknown";

                // Role-based access control
                if (patientMedication.Medication.IsScheduledMedication)
                {
                    if (!User.IsInRole("Nursing Sister") && !User.IsInRole("Doctor"))
                    {
                        return Forbid("Only nursing sisters and doctors can delete scheduled medications.");
                    }
                }
                else
                {
                    if (!User.IsInRole("Nurse"))
                    {
                        return Forbid("Only nurses can delete non-scheduled medications.");
                    }
                }

                // Soft delete - return medication quantity to inventory
                var medication = patientMedication.Medication;
                medication.QuantityAvailable += patientMedication.QuantityAssigned;

                // Mark as deleted and inactive
                patientMedication.IsDeleted = true;
                patientMedication.IsActive = false;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Medication assignment removed successfully. Quantity returned to inventory.";
                return RedirectToAction("Folder", new { id = patientMedication.PatientId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting medication assignment: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while removing the medication assignment.";
                return RedirectToAction("Folder", new { id = await GetPatientIdFromAssignment(patientMedicationId) });
            }
        }

        // Helper method to get patient ID from assignment
        private async Task<int> GetPatientIdFromAssignment(int patientMedicationId)
        {
            var assignment = await _context.PatientMedications
                .FirstOrDefaultAsync(pm => pm.PatientMedicationId == patientMedicationId);
            return assignment?.PatientId ?? 0;
        }

        // Administer Scheduled Medication - updated to match AssignMedication pattern
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdministerMedication(PatientFolderViewModel model)
        {
            try
            {
                // Validate patient exists
                var patient = await _context.Patients.FindAsync(model.PatientId);
                if (patient == null)
                {
                    TempData["ErrorMessage"] = "Patient not found.";
                    return RedirectToAction("Folder", new { id = model.PatientId });
                }

                // Get the medication from existing data
                var medication = await _context.Medications.FindAsync(model.MedicationId);
                if (medication == null || !medication.IsScheduledMedication)
                {
                    TempData["ErrorMessage"] = "Scheduled medication not found.";
                    return RedirectToAction("Folder", new { id = model.PatientId });
                }

                // Check if sufficient quantity is available
                if (medication.QuantityAvailable < model.QuantityAssigned)
                {
                    TempData["ErrorMessage"] = $"Insufficient quantity available. Only {medication.QuantityAvailable} units in stock.";
                    return RedirectToAction("Folder", new { id = model.PatientId });
                }

                // Check if user is authorized (Nursing Sister or Doctor)
                if (!User.IsInRole("Nursing Sister") && !User.IsInRole("Doctor"))
                {
                    TempData["ErrorMessage"] = "Only nursing sisters and doctors can administer scheduled medications.";
                    return RedirectToAction("Folder", new { id = model.PatientId });
                }

                // Get current user (employee) ID
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Create medication assignment using PatientMedication model
                var patientMedication = new PatientMedication
                {
                    PatientId = model.PatientId,
                    MedicationId = model.MedicationId,
                    EmployeeId = currentUserId,
                    AssignmentDate = DateTime.Now,
                    Dosage = model.Dosage,
                    Frequency = model.Frequency,
                    QuantityAssigned = model.QuantityAssigned,
                    DurationDays = model.DurationDays,
                    AdministrationNotes = model.AdministrationNotes,
                    IsActive = true,
                    IsDeleted = false
                };

                // Update medication inventory - reduce available quantity
                medication.QuantityAvailable -= model.QuantityAssigned;

                _context.PatientMedications.Add(patientMedication);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{medication.Name} administered successfully to {patient.FirstName} {patient.LastName}. Quantity updated in inventory.";
                return RedirectToAction("Folder", new { id = model.PatientId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error administering medication: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while administering medication.";
                return RedirectToAction("Folder", new { id = model.PatientId });
            }
        }
    }
}
