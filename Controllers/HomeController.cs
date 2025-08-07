using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;
using OPROZ_Main.ViewModels;
using System.Diagnostics;

namespace OPROZ_Main.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Get featured services for the landing page
        var featuredServices = await _context.Services
            .Where(s => s.IsActive)
            .Include(s => s.Company)
            .OrderByDescending(s => s.CreatedAt)
            .Take(6)
            .ToListAsync();

        ViewBag.FeaturedServices = featuredServices;

        // Get subscription plans for the landing page
        var subscriptionPlans = await _context.SubscriptionPlans
            .Where(sp => sp.IsActive)
            .ToListAsync();

        // Sort by price in memory (SQLite limitation with decimal sorting)
        subscriptionPlans = subscriptionPlans.OrderBy(sp => sp.Price).Take(3).ToList();

        ViewBag.SubscriptionPlans = subscriptionPlans;

        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public async Task<IActionResult> Services()
    {
        var services = await _context.Services
            .Where(s => s.IsActive)
            .Include(s => s.Company)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToListAsync();

        // Group services by category
        var servicesByCategory = services.GroupBy(s => s.Category).ToList();
        ViewBag.ServicesByCategory = servicesByCategory;

        return View(services);
    }

    public IActionResult Blogs()
    {
        // For now, return a view with placeholder content
        // In the future, you can add a Blog model and implement blog functionality
        return View();
    }

    [HttpGet]
    public IActionResult Contact()
    {
        return View(new ContactViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Create a help query from the contact form
                var helpQuery = new HelpQuery
                {
                    Name = model.Name,
                    Email = model.Email,
                    Phone = model.Phone,
                    Subject = model.Subject,
                    Message = model.Message,
                    Category = model.Category,
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow
                };

                _context.HelpQueries.Add(helpQuery);
                await _context.SaveChangesAsync();

                ViewBag.SuccessMessage = "Thank you for contacting us! We'll get back to you soon.";
                _logger.LogInformation("Contact form submitted by {Email} with subject: {Subject}", model.Email, model.Subject);

                // Clear the form
                ModelState.Clear();
                return View(new ContactViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing contact form submission");
                ViewBag.ErrorMessage = "There was an error processing your request. Please try again.";
            }
        }

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Pricing()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
