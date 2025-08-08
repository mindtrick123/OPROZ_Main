using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.ViewModels;

namespace OPROZ_Main.Controllers
{
    public class AdminController : AdminControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalServices = await _context.Services.CountAsync(),
                ActiveServices = await _context.Services.CountAsync(s => s.IsActive),
                TotalPlans = await _context.SubscriptionPlans.CountAsync(),
                ActivePlans = await _context.SubscriptionPlans.CountAsync(p => p.IsActive),
                TotalOffers = await _context.Offers.CountAsync(),
                ActiveOffers = await _context.Offers.CountAsync(o => o.IsActive),
                TotalUsers = await _context.Users.CountAsync(),
                RecentPayments = await _context.PaymentHistories
                    .Include(p => p.User)
                    .Include(p => p.SubscriptionPlan)
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }
    }
}