using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Wellness_Wardens_Project.Data;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;
using Wellness_Wardens_Project.Services;

namespace Wellness_Wardens_Project
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("connection")));

            //Add Identity
            builder.Services.AddIdentity<Employee, IdentityRole>(options =>
            {
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Add this before building
            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            // Add base path middleware (add this early in pipeline)
            app.Use(async (context, next) =>
            {
                context.Request.PathBase = new PathString("/grp-03-20");
                await next();
            });

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            QuestPDF.Settings.License = LicenseType.Community;

            app.UseHttpsRedirection();
            app.UseStaticFiles(); 

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            //Seed database
            await SeedServices.SeedDatabase(app.Services);

            app.Run();
        }
    }
}
