using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.ViewModels.Account;

namespace Wellness_Wardens_Project.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Employee> signInManager;
        private readonly UserManager<Employee> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public AccountController(SignInManager<Employee> signInManager, UserManager<Employee> userManager, RoleManager<IdentityRole> roleManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        // Login GET
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (TempData["ReturnUrl"] != null)
            {
                ViewData["ReturnUrl"] = TempData["ReturnUrl"];
            }

            return View();
        }

        // Login POST 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.FindByNameAsync(model.Email);

            if (user != null)
            {
                var result = await signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    await userManager.ResetAccessFailedCountAsync(user);
                    var roles = await userManager.GetRolesAsync(user);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        if (roles.Contains("Admin"))
                        {
                            return RedirectToAction("Dashboard", "Administration");
                        }
                        else if (roles.Contains("Doctor"))
                        {
                            return RedirectToAction("Doctors", "Home");
                        }
                        else if (roles.Contains("Script Manager"))
                        {
                            return RedirectToAction("ScriptManager", "Dashboard");
                        }
                        else if (roles.Contains("Stock Manager"))
                        {
                            return RedirectToAction("StockManager", "Dashboard");
                        }
                        else if (roles.Contains("Nurse") || roles.Contains("Nursing Sister"))
                        {
                            return RedirectToAction("Index", "Patient");
                        }
                        else if (roles.Contains("Admission Clerk"))
                        {
                            return RedirectToAction("Index", "PatientManagement");
                        }
                        else if(roles.Contains("Ward Admin"))
                        {
                            return RedirectToAction("WardAdminDashboard", "Administration");
                        }
                    }
                }
                else
                {
                    var failedCount = await userManager.GetAccessFailedCountAsync(user);

                    if (failedCount == 1)
                    {
                        ModelState.AddModelError(string.Empty, "First failed attempt. Please check your credentials.");
                    }
                    else if (failedCount == 2)
                    {
                        ModelState.AddModelError(string.Empty, "You have 1 last attempt. Your account will be locked.");
                    }
                    else if (failedCount == 3 || failedCount > 3)
                    {
                        ModelState.AddModelError(string.Empty, "Your account is locked. Click forgot password to verify your Username.");
                    }

                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }


        // Register GET — Load roles into view model
        [HttpGet]
        public IActionResult Register()
        {
            var roles = roleManager.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Name)
                .ToList();

            var model = new RegisterViewModel
            {
                AvailableRoles = roles
            };

            return View(model);
        }


        // Register POST — Creates user & assigns role
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Force AvailableRoles to only be Admin for the view
            model.AvailableRoles = new List<string> { "Admin" };

            if (!ModelState.IsValid)
                return View(model);

            // Force the role to be Admin
            model.Role = "Admin";

            var user = new Employee
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Title = model.Title,
                Gender = model.Gender,
                Role = model.Role,
                PhoneNumber = model.PhoneNumber
            };

            var result = await userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (!await roleManager.RoleExistsAsync(model.Role))
                {
                    ModelState.AddModelError("", $"Role '{model.Role}' does not exist.");
                    return View(model);
                }

                await userManager.AddToRoleAsync(user, model.Role);

                // Add TempData for toast notification
                TempData["SuccessMessage"] = "Account created successfully!";

                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }


        //checks if the email exists in the database and redirect to change password view
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["VerifyEmailError"] = "Please enter a valid email address.";
                return RedirectToAction("Login");
            }

            var user = await userManager.FindByNameAsync(model.Email);

            if (user == null)
            {
                TempData["VerifyEmailError"] = "No account associated with this email.";
                return RedirectToAction("Login");
            }

            return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
        }


        //checks if the username has a value
        [HttpGet]
        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(Login), "Account");
            }

            return View(new ChangePasswordViewModel { Email = username });
        }

        //change password POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            // Remove old password and add new one
            var removeResult = await userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                foreach (var error in removeResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            var addResult = await userManager.AddPasswordAsync(user, model.NewPassword);
            if (addResult.Succeeded)
            {
                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("Login");
            }

            foreach (var error in addResult.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(string? returnUrl = null, string? logoutType = null)
        {
            await signInManager.SignOutAsync();

            if (logoutType == "home")
            {
                return RedirectToAction("Index", "Home");
            }

            // For regular logouts, maintain current behavior
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Login", "Account", new { ReturnUrl = returnUrl });
            }

            return RedirectToAction("Login", "Account");
        }


        //get the user info.
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Title = user.Title,
                Gender = user.Gender,
                PhoneNumber = user.PhoneNumber,
                Specialization = user.Specialization,
                Roles = await userManager.GetRolesAsync(user)
            };

            return View(model);
        }

        //Edit profile - UPDATED VERSION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Collect all validation errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                TempData["ErrorMessage"] = "Please correct the errors: " + string.Join(", ", errors);
                return RedirectToAction("Profile");
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found. Please log in again.";
                return RedirectToAction("Login");
            }

            try
            {
                // Update all user properties
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Title = model.Title;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.PhoneNumber = model.PhoneNumber;
                user.Gender = model.Gender;

                var result = await userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update profile: " +
                        string.Join(" ", result.Errors.Select(e => e.Description));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating your profile: " + ex.Message;
            }

            return RedirectToAction("Profile");
        }
    }
}
