using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;
using OPROZ_Main.Services;
using OPROZ_Main.ViewModels;

namespace OPROZ_Main.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAnalyticsService _analyticsService;
        private readonly IReportingService _reportingService;
        private readonly IUserManagementService _userManagementService;
        private readonly IAuditLogService _auditLogService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            IAnalyticsService analyticsService,
            IReportingService reportingService,
            IUserManagementService userManagementService,
            IAuditLogService auditLogService,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _analyticsService = analyticsService;
            _reportingService = reportingService;
            _userManagementService = userManagementService;
            _auditLogService = auditLogService;
            _userManager = userManager;
            _logger = logger;
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = new AdminDashboardViewModel
                {
                    Metrics = await _analyticsService.GetDashboardMetricsAsync(),
                    RevenueChartData = await _analyticsService.GetRevenueChartDataAsync(),
                    SubscriptionChartData = await _analyticsService.GetSubscriptionChartDataAsync(),
                    ServiceUsageData = await _analyticsService.GetServiceUsageDataAsync(),
                    RecentActivities = await _analyticsService.GetRecentActivitiesAsync(),
                    PaymentIssues = await _analyticsService.GetPaymentIssuesAsync(),
                    ExpiringSubscriptions = await _analyticsService.GetExpiringSubscriptionsAsync()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["Error"] = "Error loading dashboard data. Please try again.";
                return View(new AdminDashboardViewModel());
            }
        }

        // Reports
        public async Task<IActionResult> Reports()
        {
            var services = await _context.Services
                .Where(s => s.IsActive)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            ViewBag.Services = services;
            return View(new ReportFilterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(ReportFilterViewModel filter)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid filter parameters");
            }

            try
            {
                byte[] reportData = filter.ReportType switch
                {
                    ReportType.Users => await _reportingService.GenerateUserReportAsync(filter),
                    ReportType.Subscriptions => await _reportingService.GenerateSubscriptionReportAsync(filter),
                    ReportType.Payments => await _reportingService.GeneratePaymentReportAsync(filter),
                    ReportType.Services => await _reportingService.GenerateServiceReportAsync(filter),
                    ReportType.AuditLog => await _reportingService.GenerateAuditLogReportAsync(filter),
                    _ => throw new ArgumentException("Invalid report type")
                };

                var contentType = _reportingService.GetContentType(filter.ExportFormat);
                var fileName = _reportingService.GetFileName(filter.ReportType, filter.ExportFormat);

                // Log the report generation
                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogService.LogUserActionAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Generate Report",
                    $"Generated {filter.ReportType} report in {filter.ExportFormat} format",
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return File(reportData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report: {ReportType}", filter.ReportType);
                TempData["Error"] = "Error generating report. Please try again.";
                return RedirectToAction(nameof(Reports));
            }
        }

        // User Management
        public async Task<IActionResult> Users(UserSearchFilter? filter, int page = 1)
        {
            filter ??= new UserSearchFilter();
            
            try
            {
                var viewModel = await _userManagementService.GetUsersAsync(filter, page);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users list");
                TempData["Error"] = "Error loading users. Please try again.";
                return View(new UserManagementViewModel());
            }
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var viewModel = await _userManagementService.GetUserDetailsAsync(id);
                return View(viewModel);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for {UserId}", id);
                TempData["Error"] = "Error loading user details. Please try again.";
                return RedirectToAction(nameof(Users));
            }
        }

        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var viewModel = await _userManagementService.GetEditUserViewModelAsync(id);
                return View(viewModel);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit user form for {UserId}", id);
                TempData["Error"] = "Error loading user details. Please try again.";
                return RedirectToAction(nameof(Users));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await _userManagementService.GetAvailableRolesAsync();
                model.Companies = await _userManagementService.GetCompaniesAsync();
                return View(model);
            }

            try
            {
                var success = await _userManagementService.UpdateUserAsync(model);
                if (success)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    await _auditLogService.LogUserActionAsync(
                        currentUser?.Id,
                        currentUser?.Email,
                        "Update User",
                        $"Updated user details for {model.Email}",
                        Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    TempData["Success"] = "User updated successfully.";
                    return RedirectToAction(nameof(UserDetails), new { id = model.Id });
                }
                else
                {
                    TempData["Error"] = "Failed to update user. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", model.Id);
                TempData["Error"] = "Error updating user. Please try again.";
            }

            model.AvailableRoles = await _userManagementService.GetAvailableRolesAsync();
            model.Companies = await _userManagementService.GetCompaniesAsync();
            return View(model);
        }

        public async Task<IActionResult> ResetPassword(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var model = new AdminResetPasswordViewModel
            {
                UserId = user.Id,
                UserName = $"{user.FirstName} {user.LastName}"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(AdminResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var success = await _userManagementService.ResetUserPasswordAsync(model.UserId, model.NewPassword);
                if (success)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    await _auditLogService.LogUserActionAsync(
                        currentUser?.Id,
                        currentUser?.Email,
                        "Reset Password",
                        $"Reset password for user ID: {model.UserId}",
                        Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    TempData["Success"] = "Password reset successfully.";
                    return RedirectToAction(nameof(UserDetails), new { id = model.UserId });
                }
                else
                {
                    TempData["Error"] = "Failed to reset password. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", model.UserId);
                TempData["Error"] = "Error resetting password. Please try again.";
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SuspendUser(string id, string reason)
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Invalid user ID" });

            try
            {
                var success = await _userManagementService.SuspendUserAsync(id, reason);
                if (success)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    await _auditLogService.LogUserActionAsync(
                        currentUser?.Id,
                        currentUser?.Email,
                        "Suspend User",
                        $"Suspended user ID: {id}. Reason: {reason}",
                        Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    return Json(new { success = true, message = "User suspended successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to suspend user" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending user {UserId}", id);
                return Json(new { success = false, message = "Error suspending user" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReactivateUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Invalid user ID" });

            try
            {
                var success = await _userManagementService.ReactivateUserAsync(id);
                if (success)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    await _auditLogService.LogUserActionAsync(
                        currentUser?.Id,
                        currentUser?.Email,
                        "Reactivate User",
                        $"Reactivated user ID: {id}",
                        Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    return Json(new { success = true, message = "User reactivated successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to reactivate user" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating user {UserId}", id);
                return Json(new { success = false, message = "Error reactivating user" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                return Json(new { success = false, message = "Invalid parameters" });

            try
            {
                var success = await _userManagementService.AssignRoleAsync(userId, role);
                if (success)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    await _auditLogService.LogUserActionAsync(
                        currentUser?.Id,
                        currentUser?.Email,
                        "Assign Role",
                        $"Assigned role '{role}' to user ID: {userId}",
                        Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    return Json(new { success = true, message = $"Role '{role}' assigned successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to assign role" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {Role} to user {UserId}", role, userId);
                return Json(new { success = false, message = "Error assigning role" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveRole(string userId, string role)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                return Json(new { success = false, message = "Invalid parameters" });

            try
            {
                var success = await _userManagementService.RemoveRoleAsync(userId, role);
                if (success)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    await _auditLogService.LogUserActionAsync(
                        currentUser?.Id,
                        currentUser?.Email,
                        "Remove Role",
                        $"Removed role '{role}' from user ID: {userId}",
                        Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    return Json(new { success = true, message = $"Role '{role}' removed successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to remove role" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {Role} from user {UserId}", role, userId);
                return Json(new { success = false, message = "Error removing role" });
            }
        }

        // Analytics API endpoints for charts
        [HttpGet]
        public async Task<IActionResult> GetRevenueChartData(int months = 12)
        {
            try
            {
                var data = await _analyticsService.GetRevenueChartDataAsync(months);
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue chart data");
                return Json(new List<ChartData>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSubscriptionChartData(int months = 12)
        {
            try
            {
                var data = await _analyticsService.GetSubscriptionChartDataAsync(months);
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription chart data");
                return Json(new List<ChartData>());
            }
        }
    }
}