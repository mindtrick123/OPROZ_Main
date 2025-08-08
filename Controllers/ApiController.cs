using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;
using System.ComponentModel.DataAnnotations;

namespace OPROZ_Main.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiController> _logger;

        public ApiController(ApplicationDbContext context, ILogger<ApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Check if a user has a valid active subscription plan
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <returns>JSON response with boolean indicating plan validity</returns>
        [HttpGet("check-plan-validity/{userId}")]
        public async Task<IActionResult> CheckPlanValidity(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { isValid = false, message = "User ID is required" });
                }

                // Check if user exists
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return NotFound(new { isValid = false, message = "User not found" });
                }

                // Get the most recent successful payment for this user
                var latestValidPayment = await _context.PaymentHistories
                    .Include(p => p.SubscriptionPlan)
                    .Where(p => p.UserId == userId && 
                               p.Status == PaymentStatus.Success &&
                               p.SubscriptionEndDate.HasValue &&
                               p.SubscriptionEndDate > DateTime.UtcNow)
                    .OrderByDescending(p => p.SubscriptionEndDate)
                    .FirstOrDefaultAsync();

                var isValid = latestValidPayment != null;
                
                var response = new
                {
                    isValid = isValid,
                    userId = userId,
                    planName = latestValidPayment?.SubscriptionPlan?.Name,
                    expiryDate = latestValidPayment?.SubscriptionEndDate,
                    checkedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Plan validity check for user {UserId}: {IsValid}", userId, isValid);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking plan validity for user {UserId}", userId);
                return StatusCode(500, new { isValid = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all active subscription plans (public endpoint)
        /// </summary>
        /// <returns>List of active subscription plans</returns>
        [HttpGet("active-plans")]
        public async Task<IActionResult> GetActivePlans()
        {
            try
            {
                var activePlans = await _context.SubscriptionPlans
                    .Where(p => p.IsActive)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        description = p.Description,
                        price = p.Price,
                        duration = p.Duration.ToString(),
                        type = p.Type.ToString(),
                        maxUsers = p.MaxUsers,
                        maxStorage = p.MaxStorage,
                        isPopular = p.IsPopular,
                        serviceId = p.ServiceId,
                        features = p.Features
                    })
                    .ToListAsync();

                // Order by price on client side to avoid SQLite decimal ordering issue
                var orderedPlans = activePlans.OrderBy(p => p.price).ToList();

                _logger.LogInformation("Retrieved {Count} active plans", orderedPlans.Count);
                
                return Ok(new { 
                    success = true, 
                    count = orderedPlans.Count,
                    plans = orderedPlans 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active plans");
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }
    }
}