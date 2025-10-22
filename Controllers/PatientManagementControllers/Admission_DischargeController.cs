using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;
using Wellness_Wardens_Project.ViewModels.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Controllers.PatientManagementControllers
{

    public class Admission_DischargeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> userManager;

        public Admission_DischargeController(ApplicationDbContext context, UserManager<Employee> _userManager)
        {
            _context = context;
            userManager = _userManager;
        }

        /*=============================
          Admission Actions
        ===========================*/

        // GET: Admissions
        [HttpGet]
        [Authorize(Roles = "Ward Admin, Admin")]
        public IActionResult Admissions()
        {
            TempData["ReturnPage"] = "Admissions";
            
            // Base query: all active admissions (not discharged)
            var admissionsQuery = _context.PatientAdmissions
                .Include(a => a.Patient)
                .Include(a => a.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
                .Include(a => a.AssignedEmployee)
                .Where(a => !a.IsDeleted && !a.Discharges.Any()); // Added discharge filter

            List<PatientAdmission> admissions;

            if (User.IsInRole("Admin"))
            {
                // Admin sees all patients, even those without admissions
                var allPatients = _context.Patients
                    .Where(p => !p.IsDeleted)
                    .ToList();

                // Left join patients with active admissions only
                admissions = allPatients
                    .GroupJoin(admissionsQuery,
                               p => p.PatientId,
                               a => a.PatientId,
                               (p, aGroup) => aGroup.DefaultIfEmpty().FirstOrDefault() ?? new PatientAdmission
                               {
                                   Patient = p,
                                   AdmissionDate = null,
                                   Reason = null,
                                   Bed = null,
                                   AssignedEmployee = null,
                                   Discharges = null
                               })
                    .Where(a => a.Discharges == null) // Ensure we don't include discharged patients
                    .ToList();
            }
            else
            {
                // Ward Admin sees only existing active admissions
                admissions = admissionsQuery.ToList();
            }

            return View(admissions);
        }

        [HttpGet]
        [Authorize(Roles = "Ward Admin, Admin")]
        public IActionResult Details(int? id, int? patientId)
        {
            PatientAdmission admission;

            if (id.HasValue && id.Value != 0)
            {
                // Normal admission details
                admission = _context.PatientAdmissions
                    .Include(a => a.Patient)
                    .Include(a => a.Bed)
                        .ThenInclude(b => b.Room)
                            .ThenInclude(r => r.Ward)
                    .Include(a => a.Employee)
                    .Include(a => a.AssignedEmployee)
                    .Include(a => a.Discharges)
                    .Include(a => a.PatientMovements)
                    .FirstOrDefault(a => a.AdmissionId == id.Value && !a.IsDeleted);

                if (admission == null) return NotFound();
            }
            else if (patientId.HasValue)
            {
                // For Admin: patient exists but no admission
                var patient = _context.Patients.Find(patientId.Value);
                if (patient == null) return NotFound();

                admission = new PatientAdmission
                {
                    Patient = patient,
                    AdmissionDate = default,
                    Reason = null,
                    Bed = null,
                    AssignedEmployee = null,
                    Discharges = new List<Discharge>(),
                    PatientMovements = new List<PatientMovement>()
                };
            }
            else
            {
                return NotFound();
            }

            return View(admission);
        }


        // GET: Select patient and prepare empty admission form
        [HttpGet]
        [Authorize(Roles = "Ward Admin")]
        public IActionResult SelectPatient()
        {
            // Patients
            var patients = _context.Patients
                .Where(p => !p.IsDeleted)
                .Select(p => new { p.PatientId, FullName = p.FirstName + " " + p.LastName })
                .ToList();
            ViewBag.Patients = new SelectList(patients, "PatientId", "FullName");

            // Wards
            var wards = _context.Wards
                .Where(w => !w.IsDeleted)
                .Select(w => new SelectListItem
                {
                    Value = w.WardId.ToString(),
                    Text = w.Name
                })
                .ToList();
            ViewBag.Wards = wards;

            // Employees
            var employees = _context.Employees
                .Where(e => !e.IsDeleted && (e.Role == "Doctor" || e.Role == "Nurse"))
                .Select(e => new SelectListItem
                {
                    Value = e.Id,
                    Text = e.Title + " " + e.LastName + " (" + e.Role + ")"
                })
                .ToList();

            var vm = new PatientAdmissionViewModel
            {
                AdmissionDate = DateTime.Today,
                Rooms = new List<SelectListItem>(),
                Beds = new List<SelectListItem>(),
                Employees = employees
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CheckActiveAdmission(int patientId)
        {
            bool isAdmitted = await _context.PatientAdmissions
                .AnyAsync(a => a.PatientId == patientId && !a.Discharges.Any());

            return Json(new { isAdmitted });
        }




        // POST: Patient selected, show admit section
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Ward Admin")]
        public IActionResult SelectPatient(int patientId)
        {
            return RedirectToAction("AdmitPatient", new { patientId });
        }

        // GET: Admit selected patient
        [HttpGet]
        public IActionResult AdmitPatient(int patientId)
        {
            var patient = _context.Patients.Find(patientId);
            if (patient == null) return NotFound();

            var employees = _context.Employees
                .Where(e => !e.IsDeleted && (e.Role == "Doctor" || e.Role == "Nurse"))
                .Select(e => new SelectListItem
                {
                    Value = e.Id,
                    Text = e.FirstName + " " + e.LastName + " (" + e.Role + ")"
                })
                .ToList();

            if (employees.Count == 0)
            {
                return NotFound();
            }

            var wards = _context.Wards
                .Where(w => !w.IsDeleted)
                .Select(w => new SelectListItem
                {
                    Value = w.WardId.ToString(),
                    Text = w.Name
                })
                .ToList();

            var vm = new PatientAdmissionViewModel
            {
                PatientId = patient.PatientId,
                PatientName = patient.FirstName + " " + patient.LastName,
                Wards = wards,
                Rooms = new List<SelectListItem>(),
                Beds = new List<SelectListItem>(),
                Employees = employees,
                AdmissionDate = DateTime.Today
            };

            System.Diagnostics.Debug.WriteLine("Employees count: " + employees.Count);
            return View("SelectPatient", vm);
        }


        // POST: Admit patient
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Ward Admin")]
        public async Task<IActionResult> AdmitPatient(PatientAdmissionViewModel model)
        {
            ModelState.Remove("Beds");
            ModelState.Remove("Rooms");
            ModelState.Remove("Wards");
            ModelState.Remove("PatientName");
            ModelState.Remove("AssignedEmployeeId");
            
            if (ModelState.IsValid)
            {

                var admin = await userManager.GetUserAsync(User);
                if (admin == null)
                {
                    ModelState.AddModelError("", "Current user not found.");
                    return View(model);
                }

                var admission = new PatientAdmission
                {
                    PatientId = model.PatientId,
                    EmployeeId = admin.Id,
                    AssignedEmployeeId = model.EmployeeId,
                    AdmissionDate = model.AdmissionDate,
                    Reason = model.Reason,
                    BedId = model.BedId
                };

                _context.PatientAdmissions.Add(admission);

                var bed = await _context.Beds.FindAsync(model.BedId);
                if (bed != null) bed.Status = "Occupied";

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Patient {model.PatientName} admitted successfully!";
                return RedirectToAction("SelectPatient");
            }

            // Repopulate dropdowns
            model.Wards = await _context.Wards
                .Where(w => !w.IsDeleted)
                .Select(w => new SelectListItem { Value = w.WardId.ToString(), Text = w.Name })
                .ToListAsync();

            if (model.WardId != 0)
            {
                model.Rooms = await _context.Rooms
                    .Where(r => r.WardId == model.WardId && !r.IsDeleted)
                    .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = r.RoomNumber })
                    .ToListAsync();
            }

            if (model.RoomId != 0)
            {
                model.Beds = await _context.Beds
                    .Where(b => b.RoomId == model.RoomId &&
                               !b.IsDeleted &&
                               b.Status == "Available")
                    .Select(b => new SelectListItem { Value = b.BedId.ToString(), Text = b.BedNumber })
                    .ToListAsync();
            }

            model.Employees = await _context.Employees
                .Where(e => !e.IsDeleted && (e.Role == "Doctor" || e.Role == "Nurse"))
                .Select(e => new SelectListItem
                {
                    Value = e.Id,
                    Text = e.FirstName + " " + e.LastName + " (" + e.Role + ")"
                })
                .ToListAsync();

            return View("SelectPatient", model);
        }

        // AJAX endpoints for cascading dropdowns
        public JsonResult GetRooms(int wardId)
        {
            var rooms = _context.Rooms
                .Where(r => r.WardId == wardId && !r.IsDeleted)
                .Select(r => new { r.RoomId, r.RoomNumber })
                .ToList();
            return Json(rooms);
        }

        public JsonResult GetBeds(int roomId)
        {
            var beds = _context.Beds
                .Where(b => b.RoomId == roomId &&
                           !b.IsDeleted &&
                           b.Status == "Available")
                .Select(b => new { b.BedId, b.BedNumber })
                .ToList();
            return Json(beds);
        }

        [HttpGet]
        [Authorize(Roles = "Ward Admin")]
        public async Task<IActionResult> EditAdmission(int id)
        {
            var admission = await _context.PatientAdmissions
                .Include(a => a.Patient)
                .Include(a => a.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
                .Include(a => a.AssignedEmployee)
                .FirstOrDefaultAsync(a => a.AdmissionId == id && !a.IsDeleted);

            if (admission == null)
                return NotFound();

            int wardId = admission.Bed?.Room?.WardId ?? 0;
            int roomId = admission.Bed?.RoomId ?? 0;

            // Get available beds + current bed (even if occupied)
            var bedsQuery = _context.Beds
                .Where(b => b.RoomId == roomId &&
                           !b.IsDeleted &&
                           (b.Status == "Available" || b.BedId == admission.BedId)); // Available OR current bed

            var model = new PatientAdmissionViewModel
            {
                AdmissionId = admission.AdmissionId,
                PatientId = admission.PatientId,
                WardId = wardId,
                RoomId = roomId,
                BedId = admission.BedId,
                AssignedEmployeeId = admission.AssignedEmployeeId,
                AdmissionDate = admission.AdmissionDate,
                Reason = admission.Reason,

                Wards = await _context.Wards
                    .Where(w => !w.IsDeleted)
                    .Select(w => new SelectListItem { Value = w.WardId.ToString(), Text = w.Name })
                    .ToListAsync(),

                Rooms = await _context.Rooms
                    .Where(r => r.WardId == wardId && !r.IsDeleted)
                    .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = r.RoomNumber })
                    .ToListAsync(),

                Beds = await bedsQuery
                    .Select(b => new SelectListItem
                    {
                        Value = b.BedId.ToString(),
                        Text = b.BedNumber + (b.Status == "Occupied" ? " (Currently Occupied)" : "")
                    })
                    .ToListAsync(),

                Employees = await _context.Employees
                    .Where(e => !e.IsDeleted && (e.Role == "Doctor" || e.Role == "Nurse"))
                    .Select(e => new SelectListItem { Value = e.Id, Text = $"{e.FirstName} {e.LastName} ({e.Role})" })
                    .ToListAsync()
            };

            return PartialView("_EditAdmissionPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Ward Admin")]
        public async Task<IActionResult> EditAdmission(PatientAdmissionViewModel model)
        {
            ModelState.Remove("Beds");
            ModelState.Remove("Rooms");
            ModelState.Remove("Wards");
            ModelState.Remove("PatientName");

            if (ModelState.IsValid)
            {
                var admission = await _context.PatientAdmissions.Include(a => a.Bed)
                    .FirstOrDefaultAsync(a => a.AdmissionId == model.AdmissionId);

                if (admission == null)
                {
                    TempData["ErrorMessage"] = "Admission not found.";
                    return RedirectToAction("Admissions");
                }

                // Update bed status if changed
                if (admission.BedId != model.BedId)
                {
                    // Free up the old bed
                    var oldBed = await _context.Beds.FindAsync(admission.BedId);
                    if (oldBed != null)
                    {
                        oldBed.Status = "Available";
                        _context.Beds.Update(oldBed);
                    }

                    // Occupy the new bed
                    var newBed = await _context.Beds.FindAsync(model.BedId);
                    if (newBed != null)
                    {
                        newBed.Status = "Occupied";
                        _context.Beds.Update(newBed);
                    }
                }

                admission.BedId = model.BedId;
                admission.AssignedEmployeeId = model.AssignedEmployeeId;
                admission.AdmissionDate = model.AdmissionDate;
                admission.Reason = model.Reason;

                _context.PatientAdmissions.Update(admission);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Admission updated successfully!";
                return RedirectToAction("Admissions");
            }

            TempData["ErrorMessage"] = "Please fix validation errors.";
            return RedirectToAction("Admissions");
        }

        // GET: /PatientManagement/Discharges
        public async Task<IActionResult> Discharges()
        {
            TempData["ReturnPage"] = "Discharges";

            var dischargedPatients = await _context.PatientAdmissions
                .Include(a => a.Patient)
                .Include(a => a.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
                .Include(a => a.AssignedEmployee)
                .Include(a => a.Discharges)
                .Where(a => a.Discharges.Any()) // Only discharged
                .OrderByDescending(a => a.Discharges.Max(d => d.DischargeDate)) // Latest first
                .ToListAsync();

            return View(dischargedPatients);
        }

        public async Task<IActionResult> ManageOccupiedBeds()
        {
            var occupiedBeds = await _context.Beds
                .Where(b => !b.IsDeleted &&
                            b.PatientAdmissions.Any(pa => !pa.Discharges.Any())) // Only beds with active admissions
                .Include(b => b.Room)
                    .ThenInclude(r => r.Ward)
                .Include(b => b.PatientAdmissions)
                    .ThenInclude(pa => pa.Patient) // Include patient info
                .Include(b => b.PatientAdmissions)
                    .ThenInclude(pa => pa.AssignedEmployee) // Include assigned staff
                .ToListAsync();

            return View(occupiedBeds);
        }


    }
}
