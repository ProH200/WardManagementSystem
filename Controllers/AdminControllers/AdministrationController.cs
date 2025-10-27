using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.ViewModels;
using Wellness_Wardens_Project.ViewModels.AdminSubsystem;
using Wellness_Wardens_Project.ViewModels.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Controllers.AdminControllers
{
    public class AdministrationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdministrationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Dashboard()
        {
        //    if (!User.IsInRole("Admin"))
        //    {
        //        ViewBag.ErrorMessage = "Unauthorized access denied.";
        //        return View("Unauthorized");
        //    }

            var model = new DashboardViewModel
            {
                TotalEmployees = _context.Employees.Count(e => !e.IsDeleted),
                TotalWards = _context.Wards.Count(w => !w.IsDeleted),
                TotalRooms = _context.Rooms.Count(r => !r.IsDeleted)
            };

            return View(model);
        }

        [Authorize(Roles = "Ward Admin")]
        public async Task<IActionResult> WardAdminDashboard()
        {
            var totalBeds = await _context.Beds.CountAsync(b => !b.IsDeleted);
            var occupiedBeds = await _context.PatientAdmissions
                                             .CountAsync(a => !a.IsDeleted &&
                                                !_context.Discharges.Any(d => d.PatientId == a.PatientId));
            var activeAdmissions = occupiedBeds;
            var totalAdmissions = await _context.PatientAdmissions.CountAsync(a => !a.IsDeleted);
            var totalDischarges = await _context.Discharges.CountAsync(d => !d.IsDeleted);
            var totalPatients = await _context.Patients.CountAsync(p => !p.IsDeleted);

            // Get recent activities
            var recentActivities = await GetRecentActivities();

            var dashboard = new WardAdminDashboardViewModel
            {
                TotalPatients = totalPatients,
                TotalAdmissions = totalAdmissions,
                ActiveAdmissions = activeAdmissions,
                TotalDischarges = totalDischarges,
                TotalBeds = totalBeds,
                OccupiedBeds = occupiedBeds,
                AvailableBeds = totalBeds - occupiedBeds,
                RecentActivities = recentActivities
            };

            return View(dashboard);
        }

        private async Task<List<RecentActivity>> GetRecentActivities()
        {
            var recentActivities = new List<RecentActivity>();

            // Get recent admissions (last 7 days)
            var recentAdmissions = await _context.PatientAdmissions
                .Include(a => a.Patient)
                .Include(a => a.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
                .Include(a => a.AssignedEmployee)
                .Where(a => !a.IsDeleted && a.AdmissionDate >= DateTime.Now.AddDays(-7))
                .OrderByDescending(a => a.AdmissionDate)
                .Take(1)
                .ToListAsync();

            foreach (var admission in recentAdmissions)
            {
                recentActivities.Add(new RecentActivity
                {
                    Type = "Admission",
                    Title = "New Patient Admission",
                    Description = $"{admission.Patient.FirstName} {admission.Patient.LastName} admitted to {admission.Bed?.Room?.Ward?.Name ?? "Ward"} - Room {admission.Bed?.Room?.RoomNumber}",
                    Timestamp = admission.AdmissionDate ?? DateTime.Now
                });
            }

            // Get recent discharges (last 7 days)
            var recentDischarges = await _context.Discharges
                .Include(d => d.PatientAdmission)
                    .ThenInclude(a => a.Patient)
                .Where(d => !d.IsDeleted && d.DischargeDate >= DateTime.Now.AddDays(-7))
                .OrderByDescending(d => d.DischargeDate)
                .Take(1)
                .ToListAsync();

            foreach (var discharge in recentDischarges)
            {
                recentActivities.Add(new RecentActivity
                {
                    Type = "Discharge",
                    Title = "Patient Discharged",
                    Description = $"{discharge.PatientAdmission.Patient.FirstName} {discharge.PatientAdmission.Patient.LastName} was discharged",
                    Timestamp = discharge.DischargeDate
                });
            }

            // Get recent patient registrations (last 7 days) - Add CreatedDate to Patient model for real data
            var recentPatients = await _context.Patients
                .Where(p => !p.IsDeleted && p.PatientId > 0) // You might need to add a CreatedDate field
                .OrderByDescending(p => p.PatientId)
                .Take(1)
                .ToListAsync();

            foreach (var patient in recentPatients)
            {
                // If you add a CreatedDate field to Patient model, use that instead
                recentActivities.Add(new RecentActivity
                {
                    Type = "Registration",
                    Title = "New Patient Registered",
                    Description = $"{patient.FirstName} {patient.LastName} added to the system",
                    Timestamp = DateTime.Now.AddDays(-new Random().Next(0, 3)) // Use actual CreatedDate when available
                });
            }

            // Get recent bed transfers/movements
            var recentMovements = await _context.PatientMovements
                .Include(pm => pm.PatientAdmission)
                    .ThenInclude(pa => pa.Patient)
                .Include(pm => pm.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
                .Where(pm => !pm.IsDeleted && pm.Date >= DateTime.Now.AddDays(-7))
                .OrderByDescending(pm => pm.Date)
                .Take(1)
                .ToListAsync();

            foreach (var movement in recentMovements)
            {
                recentActivities.Add(new RecentActivity
                {
                    Type = "Transfer",
                    Title = "Patient Transferred",
                    Description = $"{movement.PatientAdmission.Patient.FirstName} {movement.PatientAdmission.Patient.LastName} moved to {movement.Bed?.Room?.Ward?.Name} - Room {movement.Bed?.Room?.RoomNumber}",
                    Timestamp = movement.Date
                });
            }

            // Get recent medication assignments
            var recentMedications = await _context.PatientMedications
                .Include(pm => pm.Patient)
                .Include(pm => pm.Medication)
                .Include(pm => pm.Employee)
                .Where(pm => !pm.IsDeleted && pm.AssignmentDate >= DateTime.Now.AddDays(-7))
                .OrderByDescending(pm => pm.AssignmentDate)
                .Take(1)
                .ToListAsync();

            foreach (var medication in recentMedications)
            {
                recentActivities.Add(new RecentActivity
                {
                    Type = "Medication",
                    Title = "Medication Assigned",
                    Description = $"{medication.Medication.Name} assigned to {medication.Patient.FirstName} {medication.Patient.LastName}",
                    Timestamp = medication.AssignmentDate
                });
            }

            // Sort all activities by timestamp and take the most recent 5
            return recentActivities
                .OrderByDescending(a => a.Timestamp)
                .Take(5)
                .ToList();
        }
    }
}
