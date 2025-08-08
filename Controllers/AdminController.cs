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

        // Offers Management
        public async Task<IActionResult> Offers(string? search, int page = 1, string? filterType = null, bool? isActive = null)
        {
            try
            {
                var query = _context.Offers.Include(o => o.Service).AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(o => o.Name.Contains(search) || o.Code.Contains(search) || o.Description!.Contains(search));
                }

                if (!string.IsNullOrEmpty(filterType) && Enum.TryParse<OfferType>(filterType, out var offerType))
                {
                    query = query.Where(o => o.Type == offerType);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(o => o.IsActive == isActive.Value);
                }

                query = query.OrderByDescending(o => o.CreatedAt);

                var pageSize = 10;
                var totalCount = await query.CountAsync();
                var offers = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                ViewBag.Search = search;
                ViewBag.FilterType = filterType;
                ViewBag.IsActive = isActive;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.TotalCount = totalCount;

                return View(offers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading offers");
                TempData["Error"] = "Error loading offers. Please try again.";
                return View(new List<Offer>());
            }
        }

        public async Task<IActionResult> OfferDetails(int id)
        {
            try
            {
                var offer = await _context.Offers
                    .Include(o => o.Service)
                    .Include(o => o.PaymentHistories)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (offer == null)
                    return NotFound();

                return View(offer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading offer details for {OfferId}", id);
                TempData["Error"] = "Error loading offer details. Please try again.";
                return RedirectToAction(nameof(Offers));
            }
        }

        public async Task<IActionResult> CreateOffer()
        {
            var services = await _context.Services
                .Where(s => s.IsActive)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            ViewBag.Services = services;
            return View(new Offer { StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(30) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOffer(Offer offer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if code already exists
                    var existingOffer = await _context.Offers.FirstOrDefaultAsync(o => o.Code == offer.Code);
                    if (existingOffer != null)
                    {
                        ModelState.AddModelError("Code", "An offer with this code already exists.");
                    }
                    else
                    {
                        offer.CreatedAt = DateTime.UtcNow;
                        _context.Offers.Add(offer);
                        await _context.SaveChangesAsync();

                        var currentUser = await _userManager.GetUserAsync(User);
                        await _auditLogService.LogUserActionAsync(
                            currentUser?.Id,
                            currentUser?.Email,
                            "Create Offer",
                            $"Created offer: {offer.Name} ({offer.Code})",
                            Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                        );

                        TempData["Success"] = "Offer created successfully.";
                        return RedirectToAction(nameof(Offers));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating offer");
                    TempData["Error"] = "Error creating offer. Please try again.";
                }
            }

            var services = await _context.Services
                .Where(s => s.IsActive)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            ViewBag.Services = services;
            return View(offer);
        }

        public async Task<IActionResult> EditOffer(int id)
        {
            try
            {
                var offer = await _context.Offers.FindAsync(id);
                if (offer == null)
                    return NotFound();

                var services = await _context.Services
                    .Where(s => s.IsActive)
                    .Select(s => new { s.Id, s.Name })
                    .ToListAsync();

                ViewBag.Services = services;
                return View(offer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading offer for edit {OfferId}", id);
                TempData["Error"] = "Error loading offer. Please try again.";
                return RedirectToAction(nameof(Offers));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOffer(int id, Offer offer)
        {
            if (id != offer.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if code already exists for other offers
                    var existingOffer = await _context.Offers.FirstOrDefaultAsync(o => o.Code == offer.Code && o.Id != id);
                    if (existingOffer != null)
                    {
                        ModelState.AddModelError("Code", "An offer with this code already exists.");
                    }
                    else
                    {
                        offer.UpdatedAt = DateTime.UtcNow;
                        _context.Update(offer);
                        await _context.SaveChangesAsync();

                        var currentUser = await _userManager.GetUserAsync(User);
                        await _auditLogService.LogUserActionAsync(
                            currentUser?.Id,
                            currentUser?.Email,
                            "Update Offer",
                            $"Updated offer: {offer.Name} ({offer.Code})",
                            Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                        );

                        TempData["Success"] = "Offer updated successfully.";
                        return RedirectToAction(nameof(Offers));
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await OfferExistsAsync(offer.Id))
                        return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating offer {OfferId}", id);
                    TempData["Error"] = "Error updating offer. Please try again.";
                }
            }

            var services = await _context.Services
                .Where(s => s.IsActive)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            ViewBag.Services = services;
            return View(offer);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOffer(int id)
        {
            try
            {
                var offer = await _context.Offers.FindAsync(id);
                if (offer == null)
                    return Json(new { success = false, message = "Offer not found." });

                // Check if offer is being used
                var isUsed = await _context.PaymentHistories.AnyAsync(p => p.OfferId == id);
                if (isUsed)
                {
                    return Json(new { success = false, message = "Cannot delete offer that has been used in payments." });
                }

                _context.Offers.Remove(offer);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogService.LogUserActionAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Delete Offer",
                    $"Deleted offer: {offer.Name} ({offer.Code})",
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Json(new { success = true, message = "Offer deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting offer {OfferId}", id);
                return Json(new { success = false, message = "Error deleting offer. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleOfferStatus(int id)
        {
            try
            {
                var offer = await _context.Offers.FindAsync(id);
                if (offer == null)
                    return Json(new { success = false, message = "Offer not found." });

                offer.IsActive = !offer.IsActive;
                offer.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogService.LogUserActionAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Toggle Offer Status",
                    $"Set offer {offer.Name} ({offer.Code}) status to {(offer.IsActive ? "Active" : "Inactive")}",
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Json(new { success = true, message = $"Offer {(offer.IsActive ? "activated" : "deactivated")} successfully.", isActive = offer.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling offer status {OfferId}", id);
                return Json(new { success = false, message = "Error updating offer status. Please try again." });
            }
        }

        private async Task<bool> OfferExistsAsync(int id)
        {
            return await _context.Offers.AnyAsync(e => e.Id == id);
        }

        // Services Management
        public async Task<IActionResult> Services(string? search, int page = 1, bool? isActive = null, bool? isFeatured = null)
        {
            try
            {
                var query = _context.Services.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s => s.Name.Contains(search) || s.Description!.Contains(search) || s.ShortDescription!.Contains(search));
                }

                if (isActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == isActive.Value);
                }

                if (isFeatured.HasValue)
                {
                    query = query.Where(s => s.IsFeatured == isFeatured.Value);
                }

                query = query.OrderBy(s => s.DisplayOrder).ThenByDescending(s => s.CreatedAt);

                var pageSize = 10;
                var totalCount = await query.CountAsync();
                var services = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                ViewBag.Search = search;
                ViewBag.IsActive = isActive;
                ViewBag.IsFeatured = isFeatured;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.TotalCount = totalCount;

                return View(services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading services");
                TempData["Error"] = "Error loading services. Please try again.";
                return View(new List<Service>());
            }
        }

        public async Task<IActionResult> ServiceDetails(int id)
        {
            try
            {
                var service = await _context.Services
                    .Include(s => s.SubscriptionPlans)
                    .Include(s => s.Offers)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (service == null)
                    return NotFound();

                return View(service);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading service details for {ServiceId}", id);
                TempData["Error"] = "Error loading service details. Please try again.";
                return RedirectToAction(nameof(Services));
            }
        }

        public IActionResult CreateService()
        {
            return View(new Service { DisplayOrder = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(Service service)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    service.CreatedAt = DateTime.UtcNow;
                    _context.Services.Add(service);
                    await _context.SaveChangesAsync();

                    var currentUser = await _userManager.GetUserAsync(User);
                    await _auditLogService.LogUserActionAsync(
                        currentUser?.Id,
                        currentUser?.Email,
                        "Create Service",
                        $"Created service: {service.Name}",
                        Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    TempData["Success"] = "Service created successfully.";
                    return RedirectToAction(nameof(Services));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating service");
                    TempData["Error"] = "Error creating service. Please try again.";
                }
            }

            return View(service);
        }

        public async Task<IActionResult> EditService(int id)
        {
            try
            {
                var service = await _context.Services.FindAsync(id);
                if (service == null)
                    return NotFound();

                return View(service);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading service for edit {ServiceId}", id);
                TempData["Error"] = "Error loading service. Please try again.";
                return RedirectToAction(nameof(Services));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(int id, Service service)
        {
            if (id != service.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    service.UpdatedAt = DateTime.UtcNow;
                    _context.Update(service);
                    await _context.SaveChangesAsync();

                    var currentUser = await _userManager.GetUserAsync(User);
                    await _auditLogService.LogUserActionAsync(
                        currentUser?.Id,
                        currentUser?.Email,
                        "Update Service",
                        $"Updated service: {service.Name}",
                        Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    TempData["Success"] = "Service updated successfully.";
                    return RedirectToAction(nameof(Services));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ServiceExistsAsync(service.Id))
                        return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating service {ServiceId}", id);
                    TempData["Error"] = "Error updating service. Please try again.";
                }
            }

            return View(service);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteService(int id)
        {
            try
            {
                var service = await _context.Services.FindAsync(id);
                if (service == null)
                    return Json(new { success = false, message = "Service not found." });

                // Check if service is being used
                var hasSubscriptionPlans = await _context.SubscriptionPlans.AnyAsync(sp => sp.ServiceId == id);
                var hasOffers = await _context.Offers.AnyAsync(o => o.ServiceId == id);

                if (hasSubscriptionPlans || hasOffers)
                {
                    return Json(new { success = false, message = "Cannot delete service that has associated subscription plans or offers." });
                }

                _context.Services.Remove(service);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogService.LogUserActionAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Delete Service",
                    $"Deleted service: {service.Name}",
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Json(new { success = true, message = "Service deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service {ServiceId}", id);
                return Json(new { success = false, message = "Error deleting service. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleServiceStatus(int id)
        {
            try
            {
                var service = await _context.Services.FindAsync(id);
                if (service == null)
                    return Json(new { success = false, message = "Service not found." });

                service.IsActive = !service.IsActive;
                service.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogService.LogUserActionAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Toggle Service Status",
                    $"Set service {service.Name} status to {(service.IsActive ? "Active" : "Inactive")}",
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Json(new { success = true, message = $"Service {(service.IsActive ? "activated" : "deactivated")} successfully.", isActive = service.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling service status {ServiceId}", id);
                return Json(new { success = false, message = "Error updating service status. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleServiceFeatured(int id)
        {
            try
            {
                var service = await _context.Services.FindAsync(id);
                if (service == null)
                    return Json(new { success = false, message = "Service not found." });

                service.IsFeatured = !service.IsFeatured;
                service.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogService.LogUserActionAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Toggle Service Featured",
                    $"Set service {service.Name} featured status to {(service.IsFeatured ? "Featured" : "Not Featured")}",
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Json(new { success = true, message = $"Service {(service.IsFeatured ? "marked as featured" : "unmarked as featured")} successfully.", isFeatured = service.IsFeatured });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling service featured status {ServiceId}", id);
                return Json(new { success = false, message = "Error updating service featured status. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReorderServices(int[] serviceIds)
        {
            try
            {
                for (int i = 0; i < serviceIds.Length; i++)
                {
                    var service = await _context.Services.FindAsync(serviceIds[i]);
                    if (service != null)
                    {
                        service.DisplayOrder = i;
                        service.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogService.LogUserActionAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Reorder Services",
                    $"Reordered {serviceIds.Length} services",
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Json(new { success = true, message = "Services reordered successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering services");
                return Json(new { success = false, message = "Error reordering services. Please try again." });
            }
        }

        private async Task<bool> ServiceExistsAsync(int id)
        {
            return await _context.Services.AnyAsync(e => e.Id == id);
        }

        // Help Queries Management
        public async Task<IActionResult> HelpQueries(string? search, int page = 1, QueryStatus? status = null, QueryPriority? priority = null, string? category = null)
        {
            try
            {
                var query = _context.HelpQueries.Include(h => h.User).AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(h => h.Subject.Contains(search) || h.Message.Contains(search) || h.Name.Contains(search) || h.Email.Contains(search));
                }

                if (status.HasValue)
                {
                    query = query.Where(h => h.Status == status.Value);
                }

                if (priority.HasValue)
                {
                    query = query.Where(h => h.Priority == priority.Value);
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(h => h.Category == category);
                }

                query = query.OrderByDescending(h => h.CreatedAt);

                var pageSize = 15;
                var totalCount = await query.CountAsync();
                var helpQueries = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                // Get available categories
                var categories = await _context.HelpQueries
                    .Where(h => !string.IsNullOrEmpty(h.Category))
                    .Select(h => h.Category!)
                    .Distinct()
                    .ToListAsync();

                ViewBag.Search = search;
                ViewBag.Status = status;
                ViewBag.Priority = priority;
                ViewBag.Category = category;
                ViewBag.Categories = categories;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.TotalCount = totalCount;

                return View(helpQueries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading help queries");
                TempData["Error"] = "Error loading help queries. Please try again.";
                return View(new List<HelpQuery>());
            }
        }

        public async Task<IActionResult> HelpQueryDetails(int id)
        {
            try
            {
                var helpQuery = await _context.HelpQueries
                    .Include(h => h.User)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (helpQuery == null)
                    return NotFound();

                return View(helpQuery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading help query details for {QueryId}", id);
                TempData["Error"] = "Error loading help query details. Please try again.";
                return RedirectToAction(nameof(HelpQueries));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateHelpQuery(int id, QueryStatus status, QueryPriority priority, string? category, string? adminResponse)
        {
            try
            {
                var helpQuery = await _context.HelpQueries.FindAsync(id);
                if (helpQuery == null)
                    return NotFound();

                helpQuery.Status = status;
                helpQuery.Priority = priority;
                helpQuery.Category = category;
                helpQuery.AdminResponse = adminResponse;
                helpQuery.UpdatedAt = DateTime.UtcNow;

                if (status == QueryStatus.Resolved && helpQuery.ResolvedAt == null)
                {
                    helpQuery.ResolvedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogService.LogUserActionAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Update Help Query",
                    $"Updated help query #{id} - Status: {status}, Priority: {priority}",
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["Success"] = "Help query updated successfully.";
                return RedirectToAction(nameof(HelpQueryDetails), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating help query {QueryId}", id);
                TempData["Error"] = "Error updating help query. Please try again.";
                return RedirectToAction(nameof(HelpQueryDetails), new { id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkUpdateHelpQueries(int[] queryIds, string action, string? value)
        {
            try
            {
                if (queryIds == null || queryIds.Length == 0)
                    return Json(new { success = false, message = "No queries selected." });

                var queries = await _context.HelpQueries.Where(q => queryIds.Contains(q.Id)).ToListAsync();
                int updatedCount = 0;

                foreach (var query in queries)
                {
                    switch (action.ToLower())
                    {
                        case "status":
                            if (Enum.TryParse<QueryStatus>(value, out var status))
                            {
                                query.Status = status;
                                query.UpdatedAt = DateTime.UtcNow;
                                if (status == QueryStatus.Resolved && query.ResolvedAt == null)
                                    query.ResolvedAt = DateTime.UtcNow;
                                updatedCount++;
                            }
                            break;
                        case "priority":
                            if (Enum.TryParse<QueryPriority>(value, out var priority))
                            {
                                query.Priority = priority;
                                query.UpdatedAt = DateTime.UtcNow;
                                updatedCount++;
                            }
                            break;
                        case "category":
                            query.Category = value;
                            query.UpdatedAt = DateTime.UtcNow;
                            updatedCount++;
                            break;
                    }
                }

                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogService.LogUserActionAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Bulk Update Help Queries",
                    $"Updated {updatedCount} help queries - Action: {action}, Value: {value}",
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Json(new { success = true, message = $"Successfully updated {updatedCount} help queries." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating help queries");
                return Json(new { success = false, message = "Error updating help queries. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteHelpQuery(int id)
        {
            try
            {
                var helpQuery = await _context.HelpQueries.FindAsync(id);
                if (helpQuery == null)
                    return Json(new { success = false, message = "Help query not found." });

                _context.HelpQueries.Remove(helpQuery);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogService.LogUserActionAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Delete Help Query",
                    $"Deleted help query #{id} from {helpQuery.Email}",
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Json(new { success = true, message = "Help query deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting help query {QueryId}", id);
                return Json(new { success = false, message = "Error deleting help query. Please try again." });
            }
        }

        // Companies Management
        public async Task<IActionResult> Companies(string? search, int page = 1, bool? isActive = null)
        {
            try
            {
                var query = _context.Companies.Include(c => c.Users).Include(c => c.PaymentHistories).AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => c.Name.Contains(search) || c.Description!.Contains(search) || c.Website!.Contains(search));
                }

                if (isActive.HasValue)
                {
                    query = query.Where(c => c.IsActive == isActive.Value);
                }

                query = query.OrderByDescending(c => c.CreatedAt);

                var pageSize = 10;
                var totalCount = await query.CountAsync();
                var companies = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                ViewBag.Search = search;
                ViewBag.IsActive = isActive;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.TotalCount = totalCount;

                return View(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading companies");
                TempData["Error"] = "Error loading companies. Please try again.";
                return View(new List<Company>());
            }
        }

        public async Task<IActionResult> CompanyDetails(int id)
        {
            try
            {
                var company = await _context.Companies
                    .Include(c => c.Users)
                    .Include(c => c.PaymentHistories)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (company == null)
                    return NotFound();

                return View(company);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading company details for {CompanyId}", id);
                TempData["Error"] = "Error loading company details. Please try again.";
                return RedirectToAction(nameof(Companies));
            }
        }

        public IActionResult CreateCompany()
        {
            return View(new Company());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCompany(Company company)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    company.CreatedAt = DateTime.UtcNow;
                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();

                    var currentUser = await _userManager.GetUserAsync(User);
                    await _auditLogService.LogUserActionAsync(
                        currentUser?.Id,
                        currentUser?.Email,
                        "Create Company",
                        $"Created company: {company.Name}",
                        Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    TempData["Success"] = "Company created successfully.";
                    return RedirectToAction(nameof(Companies));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating company");
                    TempData["Error"] = "Error creating company. Please try again.";
                }
            }

            return View(company);
        }

        public async Task<IActionResult> EditCompany(int id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                    return NotFound();

                return View(company);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading company for edit {CompanyId}", id);
                TempData["Error"] = "Error loading company. Please try again.";
                return RedirectToAction(nameof(Companies));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCompany(int id, Company company)
        {
            if (id != company.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    company.UpdatedAt = DateTime.UtcNow;
                    _context.Update(company);
                    await _context.SaveChangesAsync();

                    var currentUser = await _userManager.GetUserAsync(User);
                    await _auditLogService.LogUserActionAsync(
                        currentUser?.Id,
                        currentUser?.Email,
                        "Update Company",
                        $"Updated company: {company.Name}",
                        Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    TempData["Success"] = "Company updated successfully.";
                    return RedirectToAction(nameof(Companies));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating company {CompanyId}", id);
                    TempData["Error"] = "Error updating company. Please try again.";
                }
            }

            return View(company);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleCompanyStatus(int id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                    return Json(new { success = false, message = "Company not found." });

                company.IsActive = !company.IsActive;
                company.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditLogService.LogUserActionAsync(
                    currentUser?.Id,
                    currentUser?.Email,
                    "Toggle Company Status",
                    $"Set company {company.Name} status to {(company.IsActive ? "Active" : "Inactive")}",
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Json(new { success = true, message = $"Company {(company.IsActive ? "activated" : "deactivated")} successfully.", isActive = company.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling company status {CompanyId}", id);
                return Json(new { success = false, message = "Error updating company status. Please try again." });
            }
        }
    }
}