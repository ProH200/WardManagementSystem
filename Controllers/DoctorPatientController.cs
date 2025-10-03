using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.DoctorPatientSubsystem;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.Controllers
{
    [Authorize(Roles = "Doctor")]
    
    public class DoctorController : Controller  // Changed from DoctorController to DoctorPatientController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> _userManager;

        public DoctorController(ApplicationDbContext context, UserManager<Employee> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Doctor dashboard view
        public IActionResult Doctors()
        {
            return View();
        }

        // Get logged-in doctor’s events (JSON)
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var events = await _context.ScheduleVisits
                .Where(v => !v.IsDeleted && v.EmployeeId == userId)
                .Select(v => new
                {
                    id = v.ScheduleVisitId,
                    date = v.VisitDate.ToString("yyyy-MM-dd"),
                    title = "Scheduled Visit", // Add default title
                    startTime = "09:00",       // Add default times
                    endTime = "10:00"          // Add default times
                })
                .ToListAsync();

            return Json(events);
        }

        // Add new event
        [HttpPost]
        public async Task<IActionResult> AddEvent([FromBody] ScheduleVisit model)
        {
            if (model == null || model.VisitDate == default)
                return Json(new { success = false, message = "Invalid data" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var newVisit = new ScheduleVisit
            {
                Title = model.Title,
                VisitDate = model.VisitDate,
                StartTime = TimeSpan.Parse(model.StartTime.ToString()), // Handle time conversion
                EndTime = TimeSpan.Parse(model.EndTime.ToString()),
                EmployeeId = userId,
                IsDeleted = false
            };

            _context.ScheduleVisits.Add(newVisit);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // GET: Schedule Visit Page
        public IActionResult ScheduleVisit()
        {
            return View();
        }

        // POST: Save Visit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleVisit(ScheduleVisit model)
        {
            if (!ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var newVisit = new ScheduleVisit
                {
                    VisitDate = model.VisitDate,
                    EmployeeId = userId,
                    IsDeleted = false
                };

                _context.ScheduleVisits.Add(newVisit);
                await _context.SaveChangesAsync();

                // ✅ Add toast success message
                TempData["ToastMessage"] = "Visit scheduled successfully!";
                TempData["ToastType"] = "success";

                // Redirect back to Doctor Dashboard
                return RedirectToAction("ScheduleVisit");

                
            }
            TempData["ToastMessage"] = "Invalid input. Please try again.";
            TempData["ToastType"] = "error";
            return View();

        }

        // Delete all events for a specific date
        [HttpPost]
        public async Task<IActionResult> DeleteEvent([FromBody] DeleteEventRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!DateTime.TryParse(request.VisitDate, out var visitDate))
            {
                TempData["ToastMessage"] = "Invalid date format!";
                TempData["ToastType"] = "error";
                return Json(new { success = false });
            }

            var visitsToDelete = await _context.ScheduleVisits
                .Where(v => v.EmployeeId == userId &&
                           v.VisitDate.Date == visitDate.Date &&
                           !v.IsDeleted)
                .ToListAsync();

            if (!visitsToDelete.Any())
            {
                TempData["ToastMessage"] = "No visits found for the selected date.";
                TempData["ToastType"] = "warning";
                return Json(new { success = false });
            }

            foreach (var visit in visitsToDelete)
                visit.IsDeleted = true;

            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Visits deleted successfully!";
            TempData["ToastType"] = "success";

            return Json(new { success = true });
        }

        // Delete single event by ID
        [HttpPost]
        public async Task<IActionResult> DeleteSingleEvent([FromBody] DeleteSingleEventRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var visit = await _context.ScheduleVisits
                .FirstOrDefaultAsync(v => v.ScheduleVisitId == request.EventId &&
                                         v.EmployeeId == userId &&
                                         !v.IsDeleted);

            if (visit != null)
            {
                visit.IsDeleted = true;
                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = "Event deleted successfully!";
                TempData["ToastType"] = "success";

                return Json(new { success = true });
            }

            TempData["ToastMessage"] = "Event not found!";
            TempData["ToastType"] = "error";

            return Json(new { success = false });
        }

        // Add these request models at the bottom of your controller
        public class DeleteEventRequest
        {
            public string VisitDate { get; set; }
        }

        public class DeleteSingleEventRequest
        {
            public int EventId { get; set; }
        }

        // Update Event
        [HttpPost]
        public async Task<IActionResult> UpdateEvent([FromBody] ScheduleVisit model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var visit = await _context.ScheduleVisits
                .FirstOrDefaultAsync(v => v.ScheduleVisitId == model.ScheduleVisitId &&
                                         v.EmployeeId == userId &&
                                         !v.IsDeleted);

            if (visit != null)
            {
                visit.Title = model.Title;
                visit.StartTime = TimeSpan.Parse(model.StartTime.ToString());
                visit.EndTime = TimeSpan.Parse(model.EndTime.ToString());

                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = "Event updated successfully!";
                TempData["ToastType"] = "success";

                return Json(new { success = true });
            }

            TempData["ToastMessage"] = "Event not found!";
            TempData["ToastType"] = "error";

            return Json(new { success = false });
        }
    }
}