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


        // GET: Delete instruction confirmation
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> DeleteInstructionConfirmation(int id)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var instruction = await _context.Instructions
                .Include(i => i.Patient)
                .Include(i => i.Doctor)
                .FirstOrDefaultAsync(i => i.InstructionId == id && 
                                        i.DoctorId == doctor.Id && 
                                        !i.IsDeleted);

            if (instruction == null)
            {
                TempData["ToastMessage"] = "Instruction not found or you don't have permission to delete it.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Instructions", "DoctorInstructions");
            }

            return View("~/Views/Doctor/DeleteInstructionConfirmation.cshtml", instruction);
        }

        // POST: Delete instruction (soft delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> DeleteInstruction(int id)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var instruction = await _context.Instructions
                .FirstOrDefaultAsync(i => i.InstructionId == id && 
                                        i.DoctorId == doctor.Id && 
                                        !i.IsDeleted);

            if (instruction == null)
            {
                TempData["ToastMessage"] = "Instruction not found or you don't have permission to delete it.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Instructions", "DoctorInstructions");
            }

            try
            {
                // Soft delete
                instruction.IsDeleted = true;

                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = "Instruction deleted successfully!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Instructions", "DoctorInstructions");
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Error deleting instruction: {ex.Message}";
                TempData["ToastType"] = "error";
                return RedirectToAction("DeleteInstructionConfirmation", "DoctorInstructions", new { id });
            }
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

        private bool InstructionExists(int id)
        {
            return _context.Instructions.Any(e => e.InstructionId == id && !e.IsDeleted);
        }

        // GET: Edit instruction
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> EditInstruction(int id)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var instruction = await _context.Instructions
                .Include(i => i.Patient)
                .FirstOrDefaultAsync(i => i.InstructionId == id &&
                                        i.DoctorId == doctor.Id &&
                                        !i.IsDeleted);

            if (instruction == null)
            {
                TempData["ToastMessage"] = "Instruction not found or you don't have permission to edit it.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Instructions", "DoctorInstructions");
            }

            // Get patients assigned to this doctor
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

            return View("~/Views/Doctor/EditInstruction.cshtml", instruction);
        }

        // POST: Update instruction - FIXED VERSION
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> EditInstruction(int id, [Bind("InstructionId,PatientId,InstructionType,Title,Content,Priority,ExpiryDate")] Instruction instruction)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Unauthorized();

            var existingInstruction = await _context.Instructions
                .FirstOrDefaultAsync(i => i.InstructionId == id &&
                                        i.DoctorId == doctor.Id &&
                                        !i.IsDeleted);

            if (existingInstruction == null)
            {
                TempData["ToastMessage"] = "Instruction not found or you don't have permission to edit it.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Instructions", "DoctorInstructions");
            }

            try
            {
                // Update only the fields that should be editable
                existingInstruction.PatientId = instruction.PatientId;
                existingInstruction.InstructionType = instruction.InstructionType;
                existingInstruction.Title = instruction.Title;
                existingInstruction.Content = instruction.Content;
                existingInstruction.Priority = instruction.Priority;
                existingInstruction.ExpiryDate = instruction.ExpiryDate;

                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = "Instruction updated successfully!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Instructions", "DoctorInstructions");
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Error updating instruction: {ex.Message}";
                TempData["ToastType"] = "error";

                // Reload patients for the view
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

                return View("~/Views/Doctor/EditInstruction.cshtml", instruction);
            }
        }

    }
}