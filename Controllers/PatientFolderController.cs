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
    public class PatientFolderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> _userManager;

        public PatientFolderController(ApplicationDbContext context, UserManager<Employee> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Doctor's list of assigned patients
        public async Task<IActionResult> Index()
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var patients = await _context.PatientAdmissions
                .Include(pa => pa.Patient)
                .Where(pa => pa.AssignedEmployeeId == doctor.Id &&
                           !pa.IsDeleted &&
                           !pa.Patient.IsDeleted &&
                           !pa.Discharges.Any(d => !d.IsDeleted))
                .Select(pa => pa.Patient)
                .Distinct()
                .ToListAsync();

            return View(patients);
        }

        // View individual patient folder
        public async Task<IActionResult> Details(int id)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            // Get the patient admission with all related data
            var admission = await _context.PatientAdmissions
                .Include(a => a.Patient)
                    .ThenInclude(p => p.MedicalHistories)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.Allergies)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.MedicalConditions)
                .Include(a => a.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
                .Include(a => a.AssignedEmployee)
                .FirstOrDefaultAsync(a => a.PatientId == id &&
                                          a.AssignedEmployeeId == doctor.Id &&
                                          !a.IsDeleted);

            if (admission == null)
            {
                return NotFound();
            }

            return View(admission);
        }
    }
}