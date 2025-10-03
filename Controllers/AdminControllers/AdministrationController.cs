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
            //if (!User.IsInRole("Ward Admin"))
            //{
            //    ViewBag.ErrorMessage = "Unauthorized access denied.";
            //    return View("Unauthorized");
            //}

            var totalBeds = await _context.Beds.CountAsync(b => !b.IsDeleted);
            var occupiedBeds = await _context.PatientAdmissions
                                             .CountAsync(a => !a.IsDeleted &&
                                                !_context.Discharges.Any(d => d.PatientId == a.PatientId));
            var activeAdmissions = occupiedBeds;
            var totalAdmissions = await _context.PatientAdmissions.CountAsync(a => !a.IsDeleted);
            var totalDischarges = await _context.Discharges.CountAsync(d => !d.IsDeleted);
            var totalPatients = await _context.Patients.CountAsync(p => !p.IsDeleted);

            var dashboard = new WardAdminDashboardViewModel
            {
                TotalPatients = totalPatients,
                TotalAdmissions = totalAdmissions,
                ActiveAdmissions = activeAdmissions,
                TotalDischarges = totalDischarges,
                TotalBeds = totalBeds,
                OccupiedBeds = occupiedBeds,
                AvailableBeds = totalBeds - occupiedBeds
            };

            return View(dashboard);
        }


    }
}
