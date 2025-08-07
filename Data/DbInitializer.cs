using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;

namespace OPROZ_Main.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles
            await SeedRolesAsync(roleManager);

            // Seed admin user
            await SeedAdminUserAsync(userManager, configuration);

            // Seed core services
            await SeedServicesAsync(context);

            await context.SaveChangesAsync();
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "Admin", "Manager", "User", "Support" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            var adminEmail = configuration["ApplicationSettings:AdminEmail"] ?? "admin@oproz.com";
            var adminPassword = configuration["ApplicationSettings:DefaultAdminPassword"] ?? "Admin@123456";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }

        private static async Task SeedServicesAsync(ApplicationDbContext context)
        {
            if (!await context.Services.AnyAsync())
            {
                var services = new List<Service>
                {
                    new Service
                    {
                        Name = "Web Development",
                        Description = "Custom web application development using latest technologies",
                        ShortDescription = "Professional web development services",
                        IconClass = "fas fa-code",
                        BasePrice = 999.00m,
                        IsActive = true,
                        IsFeatured = true,
                        DisplayOrder = 1,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Service
                    {
                        Name = "Mobile App Development",
                        Description = "Native and cross-platform mobile application development",
                        ShortDescription = "iOS and Android app development",
                        IconClass = "fas fa-mobile-alt",
                        BasePrice = 1499.00m,
                        IsActive = true,
                        IsFeatured = true,
                        DisplayOrder = 2,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Service
                    {
                        Name = "Digital Marketing",
                        Description = "Comprehensive digital marketing solutions including SEO, SEM, and social media",
                        ShortDescription = "Complete digital marketing solutions",
                        IconClass = "fas fa-chart-line",
                        BasePrice = 799.00m,
                        IsActive = true,
                        IsFeatured = true,
                        DisplayOrder = 3,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Service
                    {
                        Name = "Cloud Solutions",
                        Description = "Cloud infrastructure setup, migration, and management services",
                        ShortDescription = "Professional cloud services",
                        IconClass = "fas fa-cloud",
                        BasePrice = 1299.00m,
                        IsActive = true,
                        IsFeatured = false,
                        DisplayOrder = 4,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Service
                    {
                        Name = "IT Consulting",
                        Description = "Strategic IT consulting and technology roadmap planning",
                        ShortDescription = "Expert IT consulting services",
                        IconClass = "fas fa-laptop",
                        BasePrice = 599.00m,
                        IsActive = true,
                        IsFeatured = false,
                        DisplayOrder = 5,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Services.AddRangeAsync(services);
                await context.SaveChangesAsync();

                // Add subscription plans for each service
                await SeedSubscriptionPlansAsync(context, services);
            }
        }

        private static async Task SeedSubscriptionPlansAsync(ApplicationDbContext context, List<Service> services)
        {
            if (!await context.SubscriptionPlans.AnyAsync())
            {
                var plans = new List<SubscriptionPlan>();

                foreach (var service in services)
                {
                    // Basic plan
                    plans.Add(new SubscriptionPlan
                    {
                        Name = $"{service.Name} - Basic",
                        Description = $"Basic {service.Name} package",
                        ServiceId = service.Id,
                        Price = service.BasePrice ?? 0,
                        Duration = PlanDuration.Monthly,
                        Type = PlanType.Basic,
                        Features = "[\"Basic features\", \"Email support\", \"Monthly reports\"]",
                        MaxUsers = 5,
                        MaxStorage = 1024,
                        IsActive = true,
                        IsPopular = false,
                        CreatedAt = DateTime.UtcNow
                    });

                    // Standard plan
                    plans.Add(new SubscriptionPlan
                    {
                        Name = $"{service.Name} - Standard",
                        Description = $"Standard {service.Name} package with additional features",
                        ServiceId = service.Id,
                        Price = (service.BasePrice ?? 0) * 1.8m,
                        Duration = PlanDuration.Monthly,
                        Type = PlanType.Standard,
                        Features = "[\"All Basic features\", \"Priority support\", \"Weekly reports\", \"Advanced analytics\"]",
                        MaxUsers = 15,
                        MaxStorage = 5120,
                        IsActive = true,
                        IsPopular = true,
                        CreatedAt = DateTime.UtcNow
                    });

                    // Premium plan
                    plans.Add(new SubscriptionPlan
                    {
                        Name = $"{service.Name} - Premium",
                        Description = $"Premium {service.Name} package with all features",
                        ServiceId = service.Id,
                        Price = (service.BasePrice ?? 0) * 2.5m,
                        Duration = PlanDuration.Monthly,
                        Type = PlanType.Premium,
                        Features = "[\"All Standard features\", \"24/7 support\", \"Daily reports\", \"Custom integrations\", \"API access\"]",
                        MaxUsers = 50,
                        MaxStorage = 20480,
                        IsActive = true,
                        IsPopular = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await context.SubscriptionPlans.AddRangeAsync(plans);
            }
        }
    }
}