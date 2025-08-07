using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;

namespace OPROZ_Main.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles
            await SeedRolesAsync(roleManager);

            // Seed admin user
            await SeedAdminUserAsync(userManager, context);

            // Seed initial company
            await SeedCompanyAsync(context);

            // Seed initial services
            await SeedServicesAsync(context);

            // Seed subscription plans
            await SeedSubscriptionPlansAsync(context);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "SuperAdmin", "Admin", "User", "Support" };

        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        var adminEmail = "admin@oproz.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            // Get or create admin company
            var adminCompany = await context.Companies.FirstOrDefaultAsync(c => c.Name == "OPROZ Admin");
            if (adminCompany == null)
            {
                adminCompany = new Company
                {
                    Name = "OPROZ Admin",
                    Description = "System Administration Company",
                    Email = "admin@oproz.com",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                context.Companies.Add(adminCompany);
                await context.SaveChangesAsync();
            }

            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                CompanyId = adminCompany.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
            }
        }
    }

    private static async Task SeedCompanyAsync(ApplicationDbContext context)
    {
        if (!await context.Companies.AnyAsync(c => c.Name == "OPROZ Demo Company"))
        {
            var demoCompany = new Company
            {
                Name = "OPROZ Demo Company",
                Description = "A demonstration company for OPROZ platform showcasing various services and features.",
                Address = "123 Demo Street, Tech City, TC 12345",
                Phone = "+1-555-DEMO",
                Email = "demo@oproz.com",
                Website = "https://demo.oproz.com",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            context.Companies.Add(demoCompany);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedServicesAsync(ApplicationDbContext context)
    {
        if (!await context.Services.AnyAsync())
        {
            var demoCompany = await context.Companies.FirstOrDefaultAsync(c => c.Name == "OPROZ Demo Company");

            var services = new[]
            {
                new Service
                {
                    Name = "Web Development",
                    Description = "Complete web development services including frontend and backend development using modern technologies.",
                    Price = 999.99m,
                    Category = "Technology",
                    CompanyId = demoCompany?.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Service
                {
                    Name = "Mobile App Development",
                    Description = "Native and cross-platform mobile application development for iOS and Android platforms.",
                    Price = 1499.99m,
                    Category = "Technology",
                    CompanyId = demoCompany?.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Service
                {
                    Name = "Digital Marketing",
                    Description = "Comprehensive digital marketing services including SEO, social media marketing, and online advertising.",
                    Price = 799.99m,
                    Category = "Marketing",
                    CompanyId = demoCompany?.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Service
                {
                    Name = "Business Consulting",
                    Description = "Strategic business consulting services to help organizations optimize their operations and grow.",
                    Price = 1999.99m,
                    Category = "Consulting",
                    CompanyId = demoCompany?.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Service
                {
                    Name = "Cloud Solutions",
                    Description = "Cloud infrastructure setup, migration, and management services for businesses of all sizes.",
                    Price = 1299.99m,
                    Category = "Technology",
                    CompanyId = demoCompany?.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            };

            context.Services.AddRange(services);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedSubscriptionPlansAsync(ApplicationDbContext context)
    {
        if (!await context.SubscriptionPlans.AnyAsync())
        {
            var plans = new[]
            {
                new SubscriptionPlan
                {
                    Name = "Basic Plan",
                    Description = "Perfect for individuals and small teams getting started with OPROZ services.",
                    Price = 29.99m,
                    DurationInDays = 30,
                    BillingCycle = "Monthly",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Name = "Professional Plan",
                    Description = "Ideal for growing businesses with advanced features and priority support.",
                    Price = 79.99m,
                    DurationInDays = 30,
                    BillingCycle = "Monthly",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Name = "Enterprise Plan",
                    Description = "Comprehensive solution for large organizations with custom requirements.",
                    Price = 199.99m,
                    DurationInDays = 30,
                    BillingCycle = "Monthly",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Name = "Annual Basic",
                    Description = "Basic plan with annual billing - save 20% compared to monthly billing.",
                    Price = 299.99m,
                    DurationInDays = 365,
                    BillingCycle = "Yearly",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Name = "Annual Professional",
                    Description = "Professional plan with annual billing - save 20% compared to monthly billing.",
                    Price = 799.99m,
                    DurationInDays = 365,
                    BillingCycle = "Yearly",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            };

            context.SubscriptionPlans.AddRange(plans);
            await context.SaveChangesAsync();
        }
    }
}