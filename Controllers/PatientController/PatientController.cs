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

        [Authorize(Roles = "Nurse,Nursing Sister,Doctor")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var today = DateTime.Today;
                var userRole = User.IsInRole("Nursing Sister") ? "NursingSister" :
                              User.IsInRole("Nurse") ? "Nurse" :
                              User.IsInRole("Doctor") ? "Doctor" : "Other";

                var model = new PatientManagementDashboardViewModel
                {
                    TotalPatients = await _context.Patients.CountAsync(p => !p.IsDeleted),
                    TodayVisits = await _context.DoctorVisits.CountAsync(v => v.VisitDate.Date == today && !v.IsDeleted),
                    ActiveMedications = await _context.PatientMedications.CountAsync(pm => pm.IsActive && !pm.IsDeleted),
                    PendingTreatments = await _context.Treatments.CountAsync(t => !t.IsDeleted),
                    NewPatientsThisWeek = await _context.Patients.CountAsync(p => p.DateOfBirth >= DateOnly.FromDateTime(today.AddDays(-7)) && !p.IsDeleted),
                    VitalsDueToday = 0,
                };

                // Load Patients
                var patients = await _context.Patients
                    .Where(p => !p.IsDeleted)
                    .Include(p => p.DoctorVisits)
                    .ToListAsync();

                model.Patients = patients.Select(p => new PatientDashboardItemViewModel
                {
                    PatientId = p.PatientId,
                    IdentityNumber = p.IdentityNumber ?? string.Empty,
                    FirstName = p.FirstName ?? string.Empty,
                    LastName = p.LastName ?? string.Empty,
                    Gender = p.Gender ?? string.Empty,
                    DateOfBirth = p.DateOfBirth,
                    PhoneNumber = p.PhoneNumber ?? string.Empty,
                    Email = p.Email ?? string.Empty,
                    HomeAddress = p.HomeAddress ?? string.Empty,
                    EmergencyContact = p.EmergencyContact ?? string.Empty,
                    IsActive = !p.IsDeleted,
                    LastVisit = p.DoctorVisits
                        .OrderByDescending(v => v.VisitDate)
                        .FirstOrDefault()?.VisitDate
                }).ToList();

                // Load Patient Select List
                model.PatientSelectList = patients.Select(p => new PatientSelectViewModel
                {
                    PatientId = p.PatientId,
                    FirstName = p.FirstName ?? string.Empty,
                    LastName = p.LastName ?? string.Empty,
                    IdentityNumber = p.IdentityNumber ?? string.Empty
                }).ToList();

                // Load Medications based on role
                var medicationsQuery = _context.Medications.Where(m => !m.IsDeleted && m.QuantityAvailable > 0);

                if (userRole == "Nurse")
                {
                    medicationsQuery = medicationsQuery.Where(m => !m.IsScheduledMedication);
                }

                var medications = await medicationsQuery.ToListAsync();

                model.MedicationSelectList = medications.Select(m => new MedicationSelectViewModel
                {
                    MedicationId = m.MedicationId,
                    Name = m.Name ?? string.Empty,
                    QuantityAvailable = m.QuantityAvailable,
                    ScheduleLevel = m.ScheduleLevel,
                    IsScheduledMedication = m.IsScheduledMedication
                }).ToList();

                // Load Treatment Types
                model.TreatmentTypes = await _context.Treatments
                    .Where(t => !t.IsDeleted)
                    .Select(t => t.TreatmentType ?? string.Empty)
                    .Distinct()
                    .ToListAsync();

                // Load Medication Assignments
                var assignments = await _context.PatientMedications
                    .Include(pm => pm.Patient)
                    .Include(pm => pm.Medication)
                    .Include(pm => pm.Employee)
                    .Where(pm => !pm.IsDeleted && pm.IsActive)
                    .ToListAsync();

                model.MedicationAssignments = assignments.Select(pm => new MedicationAssignmentDashboardItemViewModel
                {
                    PatientMedicationId = pm.PatientMedicationId,
                    PatientId = pm.PatientId,
                    PatientName = pm.Patient != null ? $"{pm.Patient.FirstName ?? ""} {pm.Patient.LastName ?? ""}" : "",
                    MedicationId = pm.MedicationId,
                    MedicationName = pm.Medication != null ? pm.Medication.Name ?? "" : "",
                    Dosage = pm.Dosage ?? "",
                    Frequency = pm.Frequency ?? "",
                    DurationDays = pm.DurationDays,
                    QuantityAssigned = pm.QuantityAssigned,
                    AssignmentDate = pm.AssignmentDate,
                    ScheduleLevel = pm.Medication != null ? pm.Medication.ScheduleLevel : 0,
                    IsScheduled = pm.Medication != null ? pm.Medication.IsScheduledMedication : false,
                    IsActive = pm.IsActive,
                    EmployeeName = pm.Employee != null ? $"{pm.Employee.FirstName ?? ""} {pm.Employee.LastName ?? ""}" : "",
                    AdministrationNotes = pm.AdministrationNotes ?? ""
                }).ToList();

                // Load Prescriptions
                var allPrescriptions = await _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.Employee)
                    .Include(p => p.PrescriptionMedications)
                        .ThenInclude(pm => pm.Medication)
                    .Where(p => !p.IsDeleted)
                    .ToListAsync();

                // Filter prescriptions based on role
                List<Prescription> filteredPrescriptions;
                if (userRole == "Nurse")
                {
                    filteredPrescriptions = allPrescriptions
                        .Where(p => !p.PrescriptionMedications.Any(pm =>
                            pm.Medication != null && pm.Medication.IsScheduledMedication))
                        .ToList();
                }
                else if (userRole == "NursingSister")
                {
                    filteredPrescriptions = allPrescriptions
                        .Where(p => p.PrescriptionMedications.Any(pm =>
                            pm.Medication != null && pm.Medication.IsScheduledMedication))
                        .ToList();
                }
                else
                {
                    filteredPrescriptions = allPrescriptions;
                }

                model.Prescriptions = filteredPrescriptions.Select(p => new PrescriptionDashboardItemViewModel
                {
                    PrescriptionId = p.PrescriptionId,
                    PatientId = p.PatientId ?? 0,
                    PatientName = p.Patient != null ? $"{p.Patient.FirstName ?? ""} {p.Patient.LastName ?? ""}" : "",
                    DateWritten = p.DateWritten,
                    DoctorName = p.Employee != null ? $"{p.Employee.FirstName ?? ""} {p.Employee.LastName ?? ""}" : "",
                    EmployeeId = p.EmployeeId ?? "",
                    Instructions = p.Instructions ?? "",
                    Medications = p.PrescriptionMedications.Select(pm => new PrescriptionMedicationDashboardItemViewModel
                    {
                        MedicationId = pm.MedicationId,
                        Name = pm.Medication != null ? pm.Medication.Name ?? "" : "",
                        Dosage = pm.Dosage ?? ""
                    }).ToList()
                }).ToList();

                // Load Doctor Visits
                var visits = await _context.DoctorVisits
                    .Include(v => v.Patient)
                    .Where(v => !v.IsDeleted)
                    .ToListAsync();

                model.DoctorVisits = visits.Select(v => new DoctorVisitDashboardItemViewModel
                {
                    VisitId = v.VisitId,
                    PatientId = v.PatientId ?? 0,
                    PatientName = v.Patient != null ? $"{v.Patient.FirstName ?? ""} {v.Patient.LastName ?? ""}" : "",
                    VisitDate = v.VisitDate,
                    Instructions = v.Instructions ?? "",
                    DoctorName = "N/A",
                    EmployeeId = ""
                }).ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error in Dashboard: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Return an error view or redirect
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return View("Error");
            }
        }

        // ============================================================
        // FOLDER/VIEW PATIENT ACTION
        // ============================================================
        public async Task<IActionResult> Folder(int id)
        {
            try
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

                if (patient == null)
                {
                    return NotFound();
                }

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
                    filteredPrescriptions = allPrescriptions
                        .Where(p => !p.PrescriptionMedications.Any(pm =>
                            pm.Medication != null && pm.Medication.IsScheduledMedication == true))
                        .ToList();

                    recentPrescription = filteredPrescriptions
                        .OrderByDescending(p => p.DateWritten)
                        .FirstOrDefault();
                }
                else if (User.IsInRole("Nursing Sister"))
                {
                    filteredPrescriptions = allPrescriptions
                        .Where(p => p.PrescriptionMedications.Any(pm =>
                            pm.Medication != null && pm.Medication.IsScheduledMedication == true))
                        .ToList();

                    recentPrescription = filteredPrescriptions
                        .OrderByDescending(p => p.DateWritten)
                        .FirstOrDefault();
                }
                else
                {
                    filteredPrescriptions = allPrescriptions;
                    recentPrescription = allPrescriptions
                        .OrderByDescending(p => p.DateWritten)
                        .FirstOrDefault();
                }

                // Get active medication assignments
                var activeMedicationAssignments = patient.PatientMedications?
                    .Where(pm => !pm.IsDeleted && pm.IsActive)
                    .ToList() ?? new List<PatientMedication>();

                // Separate assignments by schedule type
                var nonScheduledAssignments = activeMedicationAssignments
                    .Where(pm => pm.Medication != null && !pm.Medication.IsScheduledMedication)
                    .ToList();

                var scheduledAssignments = activeMedicationAssignments
                    .Where(pm => pm.Medication != null && pm.Medication.IsScheduledMedication)
                    .ToList();

                // Separate medications by schedule type
                var nonScheduledMeds = allMedications.Where(m => m.IsScheduledMedication == false).ToList();
                var scheduledMeds = allMedications.Where(m => m.IsScheduledMedication == true).ToList();

                // Populate ViewBag with medications based on role
                if (User.IsInRole("Nurse"))
                {
                    ViewBag.NonScheduledMedications = nonScheduledMeds;
                    ViewBag.ScheduledMedications = new List<Medication>();
                }
                else if (User.IsInRole("Nursing Sister"))
                {
                    ViewBag.NonScheduledMedications = new List<Medication>();
                    ViewBag.ScheduledMedications = scheduledMeds;
                }
                else
                {
                    ViewBag.NonScheduledMedications = nonScheduledMeds;
                    ViewBag.ScheduledMedications = scheduledMeds;
                }

                // Get admissions data
                var admissions = patient.PatientAdmissions?.Where(a => !a.IsDeleted).ToList() ?? new List<PatientAdmission>();

                // Find current admission (not discharged)
                var currentAdmission = admissions
                    .Where(a => !a.Discharges.Any(d => !d.IsDeleted))
                    .OrderByDescending(a => a.AdmissionDate)
                    .FirstOrDefault();

                // Get allergies and medical conditions from junction tables
                var allergies = patient.PatientAllergies?
                    .Where(pa => !pa.IsDeleted && pa.Allergy != null)
                    .Select(pa => pa.Allergy)
                    .ToList() ?? new List<Allergy>();

                var medicalConditions = patient.PatientMedicalConditions?
                    .Where(pmc => !pmc.IsDeleted && pmc.MedicalCondition != null)
                    .Select(pmc => pmc.MedicalCondition)
                    .ToList() ?? new List<MedicalCondition>();

                var model = new PatientFolderViewModel
                {
                    Patient = patient,
                    Medications = allMedications,
                    ScheduledMedications = scheduledMeds,
                    NonScheduledAssignments = nonScheduledAssignments,
                    ScheduledAssignments = scheduledAssignments,
                    VisitNotes = visitNotes,
                    VitalSigns = patient.VitalSigns?.Where(v => !v.IsDeleted).ToList() ?? new List<VitalSigns>(),
                    Treatments = patient.Treatments?.Where(t => !t.IsDeleted).ToList() ?? new List<Treatment>(),
                    DoctorVisits = patient.DoctorVisits?.Where(d => !d.IsDeleted).ToList() ?? new List<DoctorVisit>(),
                    Allergies = allergies,
                    MedicalConditions = medicalConditions,
                    Prescriptions = filteredPrescriptions,
                    RecentPrescription = recentPrescription,
                    HasPrescriptions = filteredPrescriptions.Any(),
                    Admissions = admissions,
                    CurrentAdmission = currentAdmission,
                    IsCurrentlyAdmitted = currentAdmission != null,

                    NewVitalSign = new VitalSigns { PatientId = patient.PatientId },
                    NewTreatment = new Treatment { PatientId = patient.PatientId },
                    NewDoctorVisit = new DoctorVisit { PatientId = patient.PatientId },
                    NewPrescription = new Prescription { PatientId = patient.PatientId },
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

                // Add current location to ViewBag
                if (currentAdmission != null && currentAdmission.Bed != null)
                {
                    ViewBag.CurrentWard = currentAdmission.Bed.Room?.Ward?.Name ?? "Unknown";
                    ViewBag.CurrentRoom = currentAdmission.Bed.Room?.RoomNumber ?? "N/A";
                    ViewBag.CurrentBed = currentAdmission.Bed?.BedNumber ?? "N/A";
                    ViewBag.CurrentLocation = $"{currentAdmission.Bed.Room?.Ward?.Name ?? "Unknown"} - Room {currentAdmission.Bed.Room?.RoomNumber ?? "N/A"} - Bed {currentAdmission.Bed?.BedNumber ?? "N/A"}";
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Folder: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the patient folder.";
                return RedirectToAction("Dashboard");
            }
        }

        // ============================================================
        // INDEX ACTION - Redirect to Dashboard
        // ============================================================
        [Authorize(Roles = "Nurse,Nursing Sister")]
        public async Task<IActionResult> Index()
        {
            // Redirect to Dashboard instead of showing the old Index view
            return RedirectToAction("Dashboard");
        }

        // Get patients for dashboard
        // ============================================================
        // API ENDPOINTS FOR DASHBOARD (AJAX Calls)
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetPatients()
        {
            try
            {
                var patients = await _context.Patients
                    .Where(p => !p.IsDeleted)
                    .Include(p => p.DoctorVisits)
                    .Select(p => new
                    {
                        p.PatientId,
                        IdentityNumber = p.IdentityNumber ?? string.Empty,
                        FirstName = p.FirstName ?? string.Empty,
                        LastName = p.LastName ?? string.Empty,
                        Gender = p.Gender ?? string.Empty,
                        DateOfBirth = p.DateOfBirth,
                        PhoneNumber = p.PhoneNumber ?? string.Empty,
                        Email = p.Email ?? string.Empty,
                        HomeAddress = p.HomeAddress ?? string.Empty,
                        EmergencyContact = p.EmergencyContact ?? string.Empty,
                        IsActive = !p.IsDeleted,
                        LastVisit = p.DoctorVisits
                            .OrderByDescending(v => v.VisitDate)
                            .FirstOrDefault() != null ?
                            p.DoctorVisits.OrderByDescending(v => v.VisitDate).FirstOrDefault().VisitDate :
                            (DateTime?)null
                    })
                    .ToListAsync();

                return Json(patients);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var today = DateTime.Today;

                var stats = new
                {
                    TotalPatients = await _context.Patients.CountAsync(p => !p.IsDeleted),
                    TodayVisits = await _context.DoctorVisits.CountAsync(v => v.VisitDate.Date == today && !v.IsDeleted),
                    ActiveMeds = await _context.PatientMedications.CountAsync(pm => pm.IsActive && !pm.IsDeleted),
                    PendingTreatments = await _context.Treatments.CountAsync(t => !t.IsDeleted),
                    NewPatients = await _context.Patients.CountAsync(p => p.DateOfBirth >= DateOnly.FromDateTime(today.AddDays(-7)) && !p.IsDeleted),
                    VitalsDue = 0
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // ============================================================
        // API ENDPOINTS FOR SPECIFIC DATA
        // ============================================================

        [HttpGet]
        [Route("Patient/GetVitals/{patientId}")]
        public async Task<IActionResult> GetVitals(int patientId, string date = null)
        {
            try
            {
                Console.WriteLine($"[GetVitals] patientId={patientId}, date={date}");

                var query = _context.VitalSigns
                    .Include(v => v.Patient)
                    .Include(v => v.Employee)
                    .Where(v => v.PatientId == patientId && !v.IsDeleted);

                if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var filterDate))
                {
                    query = query.Where(v => v.RecordedDate.Date == filterDate.Date);
                }

                var vitals = await query
                    .OrderByDescending(v => v.RecordedDate)
                    .Select(v => new
                    {
                        v.VitalId,
                        v.Temperature,          // ✅ Renamed
                        v.HeartRate,
                        v.BloodPressure,
                        v.RecordedDate,
                        PatientName = v.Patient != null
                            ? (v.Patient.FirstName ?? "") + " " + (v.Patient.LastName ?? "")
                            : "Unknown",
                        EmployeeName = v.Employee != null
                            ? (v.Employee.FirstName ?? "") + " " + (v.Employee.LastName ?? "")
                            : "N/A"
                    })
                    .ToListAsync();

                Console.WriteLine($"[GetVitals] Returning {vitals.Count} records");
                return Json(vitals);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetVitals] ERROR: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTreatments(int patientId, string type = null)
        {
            try
            {
                var query = _context.Treatments
                    .Include(t => t.Patient)
                    .Where(t => t.PatientId == patientId && !t.IsDeleted);

                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(t => t.TreatmentType == type);
                }

                var treatments = await query
                    .OrderByDescending(t => t.DatePerformed)
                    .Select(t => new
                    {
                        t.TreatmentId,
                        t.TreatmentType,
                        t.DatePerformed,
                        t.IsDeleted,
                        PatientName = t.Patient != null ? $"{t.Patient.FirstName ?? ""} {t.Patient.LastName ?? ""}" : ""
                    })
                    .ToListAsync();

                return Json(treatments);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPrescriptions()
        {
            try
            {
                var userRole = User.IsInRole("Nursing Sister") ? "NursingSister" :
                              User.IsInRole("Nurse") ? "Nurse" :
                              User.IsInRole("Doctor") ? "Doctor" : "Other";

                var allPrescriptions = await _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.Employee)
                    .Include(p => p.PrescriptionMedications)
                        .ThenInclude(pm => pm.Medication)
                    .Where(p => !p.IsDeleted)
                    .ToListAsync();

                List<Prescription> filteredPrescriptions;
                if (userRole == "Nurse")
                {
                    filteredPrescriptions = allPrescriptions
                        .Where(p => !p.PrescriptionMedications.Any(pm =>
                            pm.Medication != null && pm.Medication.IsScheduledMedication))
                        .ToList();
                }
                else if (userRole == "NursingSister")
                {
                    filteredPrescriptions = allPrescriptions
                        .Where(p => p.PrescriptionMedications.Any(pm =>
                            pm.Medication != null && pm.Medication.IsScheduledMedication))
                        .ToList();
                }
                else
                {
                    filteredPrescriptions = allPrescriptions;
                }

                var result = filteredPrescriptions.Select(p => new
                {
                    p.PrescriptionId,
                    p.PatientId,
                    PatientName = p.Patient != null ? $"{p.Patient.FirstName ?? ""} {p.Patient.LastName ?? ""}" : "",
                    p.DateWritten,
                    DoctorName = p.Employee != null ? $"{p.Employee.FirstName ?? ""} {p.Employee.LastName ?? ""}" : "",
                    p.EmployeeId,
                    p.Instructions,
                    Medications = p.PrescriptionMedications.Select(pm => new
                    {
                        pm.MedicationId,
                        Name = pm.Medication != null ? pm.Medication.Name ?? "" : "",
                        pm.Dosage
                    }).ToList()
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorVisits()
        {
            try
            {
                var visits = await _context.DoctorVisits
                    .Include(v => v.Patient)
                    .Where(v => !v.IsDeleted)
                    .OrderByDescending(v => v.VisitDate)
                    .Select(v => new
                    {
                        v.VisitId,
                        PatientId = v.PatientId ?? 0,
                        PatientName = v.Patient != null ? $"{v.Patient.FirstName ?? ""} {v.Patient.LastName ?? ""}" : "",
                        v.VisitDate,
                        Instructions = v.Instructions ?? "",
                    })
                    .ToListAsync();

                return Json(visits);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // ============================================================
        // CREATE ACTIONS
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVital([FromForm] AddVitalSignsViewModel model)
        {
            try
            {
                Console.WriteLine($"[AddVital] PatientId={model.PatientId}, Temperature={model.Temperature}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = "Invalid data: " + string.Join(", ", errors) });
                }

                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == model.PatientId);
                if (patient == null)
                    return Json(new { success = false, message = "Patient not found." });

                var vital = new VitalSigns
                {
                    PatientId = model.PatientId,
                    Temperature = model.Temperature,   // ✅ Renamed
                    HeartRate = model.HeartRate,
                    BloodPressure = model.BloodPressure,
                    RecordedDate = model.RecordedDate == DateTime.MinValue ? DateTime.Now : model.RecordedDate,
                    IsDeleted = false,
                    EmployeeId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };

                _context.VitalSigns.Add(vital);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[AddVital] Saved VitalId={vital.VitalId}");
                return Json(new { success = true, message = "Vital signs recorded successfully.", vitalId = vital.VitalId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddVital] ERROR: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> History(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.PatientAdmissions)
                    .ThenInclude(pa => pa.Employee)
                .Include(p => p.PatientAdmissions)
                    .ThenInclude(pa => pa.AssignedEmployee)
                .Include(p => p.PatientAdmissions)
                    .ThenInclude(pa => pa.Bed)
                        .ThenInclude(b => b.Room)
                            .ThenInclude(r => r.Ward)
                .Include(p => p.PatientAdmissions)
                    .ThenInclude(pa => pa.Discharges)
                .Include(p => p.PatientAdmissions)
                    .ThenInclude(pa => pa.PatientMovements)
                        .ThenInclude(pm => pm.Bed)
                            .ThenInclude(b => b.Room)
                                .ThenInclude(r => r.Ward)
                .FirstOrDefaultAsync(p => p.PatientId == id && !p.IsDeleted);

            if (patient == null)
                return NotFound();

            var allAdmissions = patient.PatientAdmissions?
    .Where(a => !a.IsDeleted)
    .OrderByDescending(a => a.AdmissionDate)
    .ToList() ?? new List<PatientAdmission>();  // Already List<PatientAdmission>

            var allMovements = allAdmissions
                .SelectMany(a => a.PatientMovements ?? new List<PatientMovement>())
                .Where(m => !m.IsDeleted)
                .OrderByDescending(m => m.Date)
                .ToList() ?? new List<PatientMovement>();  // Already List<PatientMovement>

            var allDischarges = allAdmissions
                .SelectMany(a => a.Discharges ?? new List<Discharge>())
                .Where(d => !d.IsDeleted)
                .OrderByDescending(d => d.DischargeDate)
                .ToList() ?? new List<Discharge>();  // Already List<Discharge>

            var model = new PatientHistoryViewModel
            {
                Patient = patient,
                AdmissionHistory = allAdmissions,
                MovementHistory = allMovements,
                DischargeHistory = allDischarges,

                TotalAdmissions = allAdmissions.Count,
                TotalMovements = allMovements.Count,
                FirstAdmissionDate = allAdmissions.Any() ? allAdmissions.Last().AdmissionDate : null,
                LastAdmissionDate = allAdmissions.Any() ? allAdmissions.First().AdmissionDate : null
            };

            ViewBag.PatientFullName = $"{patient.FirstName} {patient.LastName}";
            ViewBag.PatientId = patient.PatientId;

            return View(model);
        }

        //// ======================
        //// ====================== VITAL SIGNS ======================
        //// POST: Add vital signs
        //// POST: Add vital signs
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AddVital(int patientId, double Temperature, int HeartRate, string BloodPressure, DateTime recordedDate)
        //{
        //    try
        //    {
        //        // Validate input
        //        if (Temperature < 30 || Temperature > 45)
        //        {
        //            TempData["ErrorMessage"] = "Temperature must be between 30°C and 45°C.";
        //            return RedirectToAction("Dashboard", new { id = patientId });
        //        }

        //        if (HeartRate < 30 || HeartRate > 250)
        //        {
        //            TempData["ErrorMessage"] = "Heart rate must be between 30 and 250 bpm.";
        //            return RedirectToAction("Dashboard", new { id = patientId });
        //        }

        //        if (string.IsNullOrEmpty(BloodPressure) || !System.Text.RegularExpressions.Regex.IsMatch(BloodPressure, @"^\d{2,3}/\d{2,3}$"))
        //        {
        //            TempData["ErrorMessage"] = "Blood pressure must be in format 120/80.";
        //            return RedirectToAction("Dashboard", new { id = patientId });
        //        }

        //        // Validate patient exists
        //        var patientExists = await _context.Patients.AnyAsync(p => p.PatientId == patientId && !p.IsDeleted);
        //        if (!patientExists)
        //        {
        //            TempData["ErrorMessage"] = $"Patient with ID {patientId} does not exist.";
        //            return RedirectToAction("Dashboard");
        //        }

        //        // Create new vital sign
        //        var vital = new VitalSigns
        //        {
        //            PatientId = patientId,
        //            Temperature = Temperature,
        //            HeartRate = HeartRate,
        //            BloodPressure = BloodPressure,
        //            RecordedDate = recordedDate == DateTime.MinValue ? DateTime.Now : recordedDate,
        //            IsDeleted = false
        //        };

        //        _context.VitalSigns.Add(vital);
        //        await _context.SaveChangesAsync();

        //        TempData["SuccessMessage"] = "Vital signs added successfully.";
        //        return RedirectToAction("Dashboard", new { id = patientId });
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error in AddVital: {ex.Message}");
        //        TempData["ErrorMessage"] = "An error occurred while saving vital signs.";
        //        return RedirectToAction("Dashboard", new { id = patientId });
        //    }
        //}

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
                existingVital.Temperature = Tempareture;
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
