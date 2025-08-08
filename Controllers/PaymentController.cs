using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;
using OPROZ_Main.Services;
using OPROZ_Main.ViewModels;
using System.Text.Json;

namespace OPROZ_Main.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRazorpayService _razorpayService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            ApplicationDbContext context,
            IRazorpayService razorpayService,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<PaymentController> logger)
        {
            _context = context;
            _razorpayService = razorpayService;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Subscribe(int planId, int? offerId = null)
        {
            var plan = await _context.SubscriptionPlans
                .Include(p => p.Service)
                .FirstOrDefaultAsync(p => p.Id == planId);

            if (plan == null)
            {
                return NotFound("Subscription plan not found.");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var offer = offerId.HasValue ? 
                await _context.Offers.FindAsync(offerId.Value) : null;

            var amount = plan.Price;
            var discountAmount = 0m;

            if (offer != null && offer.IsActive && 
                DateTime.UtcNow >= offer.StartDate && 
                DateTime.UtcNow <= offer.EndDate &&
                amount >= offer.MinOrderAmount)
            {
                discountAmount = offer.Type == OfferType.Percentage 
                    ? amount * offer.Value / 100 
                    : offer.Value;
                amount -= discountAmount;
            }

            var viewModel = new SubscriptionViewModel
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description ?? "",
                Price = plan.Price,
                Duration = plan.Duration,
                Type = plan.Type,
                Features = plan.Features ?? "",
                MaxUsers = plan.MaxUsers,
                MaxStorage = plan.MaxStorage,
                IsPopular = plan.IsPopular,
                ServiceName = plan.Service.Name,
                FeatureList = ParseFeatures(plan.Features)
            };

            ViewBag.User = user;
            ViewBag.FinalAmount = amount;
            ViewBag.DiscountAmount = discountAmount;
            ViewBag.Offer = offer;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> InitiatePayment(int planId, int? offerId = null)
        {
            try
            {
                var plan = await _context.SubscriptionPlans.FindAsync(planId);
                if (plan == null)
                {
                    return Json(new { success = false, message = "Plan not found" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var offer = offerId.HasValue ? 
                    await _context.Offers.FindAsync(offerId.Value) : null;

                var amount = plan.Price;
                var discountAmount = 0m;

                if (offer != null && offer.IsActive && 
                    DateTime.UtcNow >= offer.StartDate && 
                    DateTime.UtcNow <= offer.EndDate &&
                    amount >= offer.MinOrderAmount)
                {
                    discountAmount = offer.Type == OfferType.Percentage 
                        ? amount * offer.Value / 100 
                        : offer.Value;
                    amount -= discountAmount;
                }

                var transactionId = $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
                var orderId = _razorpayService.CreateOrderAsync(amount, "INR", transactionId);

                var payment = new PaymentViewModel
                {
                    OrderId = orderId,
                    Amount = amount,
                    Currency = "INR",
                    Name = $"{user.FirstName} {user.LastName}",
                    Email = user.Email ?? "",
                    Contact = user.PhoneNumber ?? "",
                    KeyId = _configuration["RazorpaySettings:KeyId"] ?? "",
                    SubscriptionPlanId = planId,
                    OfferId = offerId,
                    Description = $"Subscription: {plan.Name}"
                };

                return Json(new { success = true, payment });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment for plan {PlanId}", planId);
                return Json(new { success = false, message = "Payment initiation failed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyPayment([FromBody] PaymentCallbackViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid payment data" });
                }

                var isValidSignature = _razorpayService.VerifyPaymentSignature(
                    model.RazorpayOrderId, 
                    model.RazorpayPaymentId, 
                    model.RazorpaySignature);

                if (!isValidSignature)
                {
                    return Json(new { success = false, message = "Payment verification failed" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var plan = await _context.SubscriptionPlans.FindAsync(model.SubscriptionPlanId);
                if (plan == null)
                {
                    return Json(new { success = false, message = "Plan not found" });
                }

                var offer = model.OfferId.HasValue ? 
                    await _context.Offers.FindAsync(model.OfferId.Value) : null;

                var amount = plan.Price;
                var discountAmount = 0m;

                if (offer != null && offer.IsActive)
                {
                    discountAmount = offer.Type == OfferType.Percentage 
                        ? amount * offer.Value / 100 
                        : offer.Value;
                    amount -= discountAmount;
                }

                var subscriptionStart = DateTime.UtcNow;
                var subscriptionEnd = plan.Duration switch
                {
                    PlanDuration.Monthly => subscriptionStart.AddMonths(1),
                    PlanDuration.Quarterly => subscriptionStart.AddMonths(3),
                    PlanDuration.Yearly => subscriptionStart.AddYears(1),
                    _ => subscriptionStart.AddMonths(1)
                };

                var paymentHistory = new PaymentHistory
                {
                    TransactionId = $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}",
                    RazorpayPaymentId = model.RazorpayPaymentId,
                    RazorpayOrderId = model.RazorpayOrderId,
                    UserId = user.Id,
                    CompanyId = user.CompanyId,
                    SubscriptionPlanId = model.SubscriptionPlanId,
                    OfferId = model.OfferId,
                    Amount = plan.Price,
                    DiscountAmount = discountAmount,
                    FinalAmount = amount,
                    Status = PaymentStatus.Success,
                    PaymentDate = DateTime.UtcNow,
                    SubscriptionStartDate = subscriptionStart,
                    SubscriptionEndDate = subscriptionEnd,
                    Notes = $"Payment for {plan.Name} subscription"
                };

                _context.PaymentHistories.Add(paymentHistory);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment successful for user {UserId}, plan {PlanId}, payment {PaymentId}", 
                    user.Id, model.SubscriptionPlanId, model.RazorpayPaymentId);

                return Json(new { 
                    success = true, 
                    message = "Payment successful",
                    redirectUrl = Url.Action("PaymentSuccess", "Payment", new { id = paymentHistory.Id })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment");
                return Json(new { success = false, message = "Payment verification failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int id)
        {
            var payment = await _context.PaymentHistories
                .Include(p => p.SubscriptionPlan)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || payment.UserId != user.Id)
            {
                return Forbid();
            }

            return View(payment);
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var payments = await _context.PaymentHistories
                .Include(p => p.SubscriptionPlan)
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new PaymentHistoryViewModel
                {
                    Id = p.Id,
                    TransactionId = p.TransactionId,
                    RazorpayPaymentId = p.RazorpayPaymentId,
                    PlanName = p.SubscriptionPlan.Name,
                    Amount = p.Amount,
                    DiscountAmount = p.DiscountAmount,
                    FinalAmount = p.FinalAmount,
                    Status = p.Status,
                    Method = p.Method,
                    PaymentDate = p.PaymentDate,
                    SubscriptionStartDate = p.SubscriptionStartDate,
                    SubscriptionEndDate = p.SubscriptionEndDate,
                    Notes = p.Notes
                })
                .ToListAsync();

            return View(payments);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var webhookSecret = _configuration["RazorpaySettings:WebhookSecret"];
                
                // Verify webhook signature here if needed
                var eventData = JsonSerializer.Deserialize<JsonElement>(body);
                var eventType = eventData.GetProperty("event").GetString();

                _logger.LogInformation("Received Razorpay webhook: {EventType}", eventType);

                switch (eventType)
                {
                    case "payment.captured":
                        await HandlePaymentCaptured(eventData);
                        break;
                    case "payment.failed":
                        await HandlePaymentFailed(eventData);
                        break;
                    case "subscription.cancelled":
                        await HandleSubscriptionCancelled(eventData);
                        break;
                    default:
                        _logger.LogInformation("Unhandled webhook event: {EventType}", eventType);
                        break;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500);
            }
        }

        private async Task HandlePaymentCaptured(JsonElement eventData)
        {
            var payment = eventData.GetProperty("payload").GetProperty("payment").GetProperty("entity");
            var paymentId = payment.GetProperty("id").GetString();
            
            var paymentHistory = await _context.PaymentHistories
                .FirstOrDefaultAsync(p => p.RazorpayPaymentId == paymentId);

            if (paymentHistory != null && paymentHistory.Status == PaymentStatus.Pending)
            {
                paymentHistory.Status = PaymentStatus.Success;
                paymentHistory.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private async Task HandlePaymentFailed(JsonElement eventData)
        {
            var payment = eventData.GetProperty("payload").GetProperty("payment").GetProperty("entity");
            var paymentId = payment.GetProperty("id").GetString();
            
            var paymentHistory = await _context.PaymentHistories
                .FirstOrDefaultAsync(p => p.RazorpayPaymentId == paymentId);

            if (paymentHistory != null)
            {
                paymentHistory.Status = PaymentStatus.Failed;
                paymentHistory.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private async Task HandleSubscriptionCancelled(JsonElement eventData)
        {
            // Handle subscription cancellation logic here
            // This would update any subscription status in your database
            _logger.LogInformation("Subscription cancelled webhook received");
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