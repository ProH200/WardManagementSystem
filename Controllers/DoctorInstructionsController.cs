using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.PatientCareSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Controllers
{
    [Authorize(Roles = "Doctor,Nurse")]
    public class DoctorInstructionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> _userManager;

        public DoctorInstructionsController(ApplicationDbContext context, UserManager<Employee> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: List all instructions
        public async Task<IActionResult> Instructions()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var instructions = await _context.Instructions
                .Include(i => i.Doctor)
                .Include(i => i.Patient)
                .Include(i => i.CompletedBy)
                .Where(i => !i.IsDeleted)
                .OrderByDescending(i => i.CreatedDate)
                .ToListAsync();

            return View("~/Views/Doctor/Instructions.cshtml", instructions);
        }

        // GET: Create a new instruction
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreateInstruction()
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

            ViewBag.Patients = patients;

            var model = new Instruction
            {
                DoctorId = doctor.Id,
                CreatedDate = DateTime.Now
            };

            return View("~/Views/Doctor/CreateInstruction.cshtml", model);
        }

        // POST: Create a new instruction
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreateInstruction(Instruction instruction)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            try
            {
                instruction.DoctorId = doctor.Id;
                instruction.CreatedDate = DateTime.Now;

                _context.Instructions.Add(instruction);
                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = "Instruction created successfully!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Instructions", "DoctorInstructions");
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Error creating instruction: {ex.Message}";
                TempData["ToastType"] = "error";
                return RedirectToAction("CreateInstruction", "DoctorInstructions");
            }
        }

        // GET: View instruction details
        public async Task<IActionResult> InstructionDetails(int id)
        {
            var instruction = await _context.Instructions
                .Include(i => i.Doctor)
                .Include(i => i.Patient)
                .Include(i => i.CompletedBy)
                .FirstOrDefaultAsync(i => i.InstructionId == id && !i.IsDeleted);

            if (instruction == null)
            {
                TempData["ToastMessage"] = "Instruction not found.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Instructions", "DoctorInstructions");
            }

            return View("~/Views/Doctor/InstructionDetails.cshtml", instruction);
        }

        // POST: Mark instruction as completed (for nurses)
        [HttpPost]
        [Authorize(Roles = "Nurse")]
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            var nurse = await _userManager.GetUserAsync(User);
            if (nurse == null) return Unauthorized();

            var instruction = await _context.Instructions
                .FirstOrDefaultAsync(i => i.InstructionId == id && !i.IsDeleted);

            if (instruction == null)
            {
                TempData["ToastMessage"] = "Instruction not found.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Instructions", "DoctorInstructions");
            }

            try
            {
                instruction.IsCompleted = true;
                instruction.CompletedDate = DateTime.Now;
                instruction.CompletedById = nurse.Id;

                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = "Instruction marked as completed!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Instructions", "DoctorInstructions");
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Error completing instruction: {ex.Message}";
                TempData["ToastType"] = "error";
                return RedirectToAction("Instructions", "DoctorInstructions");
            }
        }
    }
}
