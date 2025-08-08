using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.ViewModels;
using System.Text.Json;

namespace OPROZ_Main.Controllers
{
    public class PlansController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlansController> _logger;

        public PlansController(ApplicationDbContext context, ILogger<PlansController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Display all active subscription plans
        /// </summary>
        /// <returns>View with active subscription plans</returns>
        public async Task<IActionResult> Index()
        {
            try
            {
                var plans = await _context.SubscriptionPlans
                    .Include(p => p.Service)
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Type)
                    .ThenBy(p => p.Price)
                    .Select(p => new SubscriptionViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description ?? "",
                        Price = p.Price,
                        Duration = p.Duration,
                        Type = p.Type,
                        Features = p.Features ?? "",
                        MaxUsers = p.MaxUsers,
                        MaxStorage = p.MaxStorage,
                        IsPopular = p.IsPopular,
                        ServiceName = p.Service.Name,
                        FeatureList = ParseFeatures(p.Features)
                    })
                    .ToListAsync();

                return View(plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading subscription plans");
                TempData["Error"] = "Error loading subscription plans. Please try again.";
                return View(new List<SubscriptionViewModel>());
            }
        }

        /// <summary>
        /// Display plans by service
        /// </summary>
        /// <param name="serviceId">Service ID to filter plans</param>
        /// <returns>View with filtered subscription plans</returns>
        public async Task<IActionResult> ByService(int serviceId)
        {
            try
            {
                var service = await _context.Services.FindAsync(serviceId);
                if (service == null)
                {
                    return NotFound();
                }

                var plans = await _context.SubscriptionPlans
                    .Include(p => p.Service)
                    .Where(p => p.IsActive && p.ServiceId == serviceId)
                    .OrderBy(p => p.Type)
                    .ThenBy(p => p.Price)
                    .Select(p => new SubscriptionViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description ?? "",
                        Price = p.Price,
                        Duration = p.Duration,
                        Type = p.Type,
                        Features = p.Features ?? "",
                        MaxUsers = p.MaxUsers,
                        MaxStorage = p.MaxStorage,
                        IsPopular = p.IsPopular,
                        ServiceName = p.Service.Name,
                        FeatureList = ParseFeatures(p.Features)
                    })
                    .ToListAsync();

                ViewBag.ServiceName = service.Name;
                return View("Index", plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading subscription plans for service {ServiceId}", serviceId);
                TempData["Error"] = "Error loading subscription plans. Please try again.";
                return View("Index", new List<SubscriptionViewModel>());
            }
        }

        private List<string> ParseFeatures(string? features)
        {
            if (string.IsNullOrEmpty(features))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(features) ?? new List<string>();
            }
            catch
            {
                // Fallback to simple comma-separated parsing
                return features.Split(',').Select(f => f.Trim()).ToList();
            }
        }
    }
}