using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.ViewModels.AdminSubsystem;

namespace Wellness_Wardens_Project.Controllers.AdminControllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Employee> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public EmployeeController(ApplicationDbContext context, UserManager<Employee> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        public IActionResult Admin()
        {
            var employees = _context.Employees
                           .Where(e => e.IsDeleted == false)
                           .ToList();
            return View(employees);
        }

        //GET Employees
        public async Task<IActionResult> ManageEmployee()
        {
            var employees = await userManager.Users
                .Where(r => r.Role != "Admin" & r.IsDeleted == false)
                .ToListAsync();
            return View(employees);
        }

        //GET AddEmployee
        [HttpGet]
        public async Task<IActionResult> AddEmployee()
        {
            var roles = await roleManager.Roles
                .Where(r => r.Name != "Admin")
                .Select(r => r.Name)
                .ToListAsync();

            var model = new AddEmployeeViewModel
            {
                AvailableRoles = roles
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployee(AddEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "User with this email already exists.");
                    model.AvailableRoles = await GetNonAdminRolesAsync();
                    return View(model);
                }

                // Set default value for Specialization before creating user
                var specialization = string.IsNullOrWhiteSpace(model.Specialization) ? "Not Specified" : model.Specialization;

                var user = new Employee
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Title = model.Title,
                    Gender = model.Gender,
                    Role = model.Role,
                    Specialization = specialization, // Use the variable here
                    PhoneNumber = model.PhoneNumber
                };

                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await userManager.AddToRoleAsync(user, model.Role);
                    }

                    TempData["ToastMessage"] = $"Employee {model.FirstName} added successfully!";
                    TempData["ToastType"] = "success";

                    return RedirectToAction("ManageEmployee");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Ensure Specialization has a value when returning to view
            if (string.IsNullOrWhiteSpace(model.Specialization))
            {
                model.Specialization = "Not Specified";
            }

            model.AvailableRoles = await GetNonAdminRolesAsync();
            return View(model);
        }

        private async Task<List<string>> GetNonAdminRolesAsync()
        {
            var NonAdminRole = await roleManager.Roles
                               .Where(r => r.Name != "Admin")
                               .Select(r => r.Name)
                               .ToListAsync();

            return NonAdminRole;
        }

        // Edit Employee - GET
        [HttpGet]
        public async Task<IActionResult> EditEmployee(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var userRoles = await userManager.GetRolesAsync(user);
            var currentRole = userRoles.FirstOrDefault();

            var viewModel = new EditEmployeeViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Title = user.Title,
                Gender = user.Gender,
                Role = currentRole,
                Specialization = user.Specialization,
                AvailableRoles = await GetNonAdminRoles()
            };

            return View(viewModel);
        }

        // Edit Employee - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(EditEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByIdAsync(model.Id);
                if (user == null)
                    return NotFound();

                if (user.Email != model.Email)
                {
                    var existingUser = await userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null && existingUser.Id != model.Id)
                    {
                        ModelState.AddModelError("Email", "A user with this email already exists.");
                        model.AvailableRoles = await GetNonAdminRoles();
                        return View(model);
                    }
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.Title = model.Title;
                user.Gender = model.Gender;
                user.PhoneNumber = model.PhoneNumber;
                user.Specialization = model.Specialization;

                var result = await userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        var currentRoles = await userManager.GetRolesAsync(user);
                        if (!currentRoles.Contains(model.Role))
                        {
                            await userManager.RemoveFromRolesAsync(user, currentRoles);
                            await userManager.AddToRoleAsync(user, model.Role);
                        }
                    }

                    TempData["ToastMessage"] = "Employee updated successfully!";
                    TempData["ToastType"] = "success";

                    return RedirectToAction("ManageEmployee");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            model.AvailableRoles = await GetNonAdminRoles();
            return View(model);
        }


        // POST: Administration/SoftDeleteEmployee - Soft delete employee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeleteEmployee(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Soft delete implementation
            user.IsDeleted = true;

            var result = await userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            // Pass a success message using TempData
            TempData["SuccessMessage"] = $"Employee {user.FirstName} {user.LastName} has been deleted successfully.";

            return RedirectToAction("ManageEmployee");
        }

        private async Task<List<string>> GetNonAdminRoles()
        {
            var NonAdminRoles = await roleManager.Roles
                                 .Where(r => r.Name != "Admin")
                                 .Select(r => r.Name)
                                 .ToListAsync();

            return NonAdminRoles;
        }
    }
}
