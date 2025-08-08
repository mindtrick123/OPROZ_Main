using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;

namespace OPROZ_Main.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubscriptionApiController> _logger;

        public SubscriptionApiController(
            ApplicationDbContext context,
            ILogger<SubscriptionApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Check if a user has a valid/active subscription plan
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <returns>Boolean indicating if the user has an active subscription</returns>
        [HttpGet("validate/{userId}")]
        public async Task<IActionResult> ValidateUserSubscription(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { isValid = false, error = "User ID is required" });
                }

                // Check if user exists
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return NotFound(new { isValid = false, error = "User not found" });
                }

                // Check for active subscription
                var now = DateTime.UtcNow;
                var hasActiveSubscription = await _context.PaymentHistories
                    .Where(p => p.UserId == userId &&
                               p.Status == PaymentStatus.Success &&
                               p.SubscriptionStartDate <= now &&
                               p.SubscriptionEndDate >= now)
                    .AnyAsync();

                return Ok(new { isValid = hasActiveSubscription });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating subscription for user {UserId}", userId);
                return StatusCode(500, new { isValid = false, error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get detailed subscription information for a user
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <returns>Detailed subscription information</returns>
        [HttpGet("details/{userId}")]
        public async Task<IActionResult> GetUserSubscriptionDetails(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { error = "User ID is required" });
                }

                var now = DateTime.UtcNow;
                var activeSubscription = await _context.PaymentHistories
                    .Include(p => p.SubscriptionPlan)
                    .Where(p => p.UserId == userId &&
                               p.Status == PaymentStatus.Success &&
                               p.SubscriptionStartDate <= now &&
                               p.SubscriptionEndDate >= now)
                    .OrderByDescending(p => p.SubscriptionEndDate)
                    .FirstOrDefaultAsync();

                if (activeSubscription == null)
                {
                    return Ok(new { 
                        isValid = false, 
                        message = "No active subscription found" 
                    });
                }

                return Ok(new
                {
                    isValid = true,
                    planName = activeSubscription.SubscriptionPlan.Name,
                    planType = activeSubscription.SubscriptionPlan.Type.ToString(),
                    startDate = activeSubscription.SubscriptionStartDate,
                    endDate = activeSubscription.SubscriptionEndDate,
                    daysRemaining = (activeSubscription.SubscriptionEndDate - now)?.Days ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription details for user {UserId}", userId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}