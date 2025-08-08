using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;
using OPROZ_Main.ViewModels;

namespace OPROZ_Main.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<DashboardController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserWithDataAsync();
            if (user == null)
                return RedirectToAction("Login", "Account");

            var viewModel = new DashboardViewModel
            {
                User = user,
                PaymentHistories = user.PaymentHistories
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(5)
                    .ToList(),
                HelpQueries = user.HelpQueries
                    .OrderByDescending(h => h.CreatedAt)
                    .Take(5)
                    .ToList(),
                ActiveSubscription = await GetActiveSubscriptionAsync(user.Id)
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Account");

            var viewModel = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                IsEmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentHistory()
        {
            var user = await GetCurrentUserWithDataAsync();
            if (user == null)
                return RedirectToAction("Login", "Account");

            var viewModel = new PaymentHistoryViewModel
            {
                PaymentHistories = user.PaymentHistories
                    .OrderByDescending(p => p.PaymentDate)
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> HelpQueries()
        {
            var user = await GetCurrentUserWithDataAsync();
            if (user == null)
                return RedirectToAction("Login", "Account");

            var viewModel = new HelpQueriesViewModel
            {
                HelpQueries = user.HelpQueries
                    .OrderByDescending(h => h.CreatedAt)
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult SubmitHelpQuery()
        {
            return View(new HelpQuerySubmissionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitHelpQuery(HelpQuerySubmissionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Account");

            var helpQuery = new HelpQuery
            {
                Subject = model.Subject,
                Message = model.Message,
                Name = $"{user.FirstName} {user.LastName}",
                Email = user.Email!,
                Phone = model.Phone,
                UserId = user.Id,
                Category = model.Category,
                Status = QueryStatus.Pending,
                Priority = model.Priority,
                CreatedAt = DateTime.UtcNow
            };

            _context.HelpQueries.Add(helpQuery);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your help query has been submitted successfully. Our team will respond shortly.";
            return RedirectToAction(nameof(HelpQueries));
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return null;

            return await _userManager.FindByIdAsync(userId);
        }

        private async Task<ApplicationUser?> GetCurrentUserWithDataAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return null;

            return await _context.Users
                .Include(u => u.PaymentHistories)
                    .ThenInclude(p => p.SubscriptionPlan)
                .Include(u => u.HelpQueries)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        private async Task<PaymentHistory?> GetActiveSubscriptionAsync(string userId)
        {
            return await _context.PaymentHistories
                .Include(p => p.SubscriptionPlan)
                .Where(p => p.UserId == userId && 
                           p.Status == PaymentStatus.Success && 
                           p.SubscriptionEndDate > DateTime.UtcNow)
                .OrderByDescending(p => p.SubscriptionEndDate)
                .FirstOrDefaultAsync();
        }
    }
}