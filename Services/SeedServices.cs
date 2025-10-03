using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.Services
{
    public class SeedServices
    {
        public static async Task SeedDatabase(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Employee>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedServices>>();

            try
            {
                logger.LogInformation("Ensuring the database is ready.");
                await context.Database.EnsureCreatedAsync();

                logger.LogInformation("Seeding roles.");
                await AddRoleAsync(roleManager, "Admin");
                await AddRoleAsync(roleManager, "Doctor");
                await AddRoleAsync(roleManager, "Nurse");
                await AddRoleAsync(roleManager, "Nursing Sister");
                await AddRoleAsync(roleManager, "Script Manager");
                await AddRoleAsync(roleManager, "Stock Manager");
                await AddRoleAsync(roleManager, "Ward Admin");
            }
            catch (DbUpdateException dbEx)
            {
                logger.LogError(dbEx, "An error occurred while saving entity changes.");

                if (dbEx.InnerException != null)
                {
                    logger.LogError("Inner exception: {Message}", dbEx.InnerException.Message);
                }
                throw;  // rethrow to fail loudly if needed
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        private static async Task AddRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
