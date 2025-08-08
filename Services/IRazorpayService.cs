using OPROZ_Main.Models;

namespace OPROZ_Main.Services
{
    public interface IRazorpayService
    {
        /// <summary>
        /// Create a Razorpay order for payment processing
        /// </summary>
        string CreateOrderAsync(decimal amount, string currency = "INR", string? receipt = null);

        /// <summary>
        /// Verify payment signature from Razorpay callback
        /// </summary>
        bool VerifyPaymentSignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature);

        /// <summary>
        /// Get payment details from Razorpay
        /// </summary>
        object GetPaymentDetailsAsync(string paymentId);

        /// <summary>
        /// Create subscription plan in Razorpay
        /// </summary>
        string CreateSubscriptionPlanAsync(SubscriptionPlan plan);

        /// <summary>
        /// Create subscription for a user
        /// </summary>
        string CreateSubscriptionAsync(string planId, string customerId, int totalCount);

        /// <summary>
        /// Cancel subscription
        /// </summary>
        bool CancelSubscriptionAsync(string subscriptionId);

        /// <summary>
        /// Fetch subscription details
        /// </summary>
        object GetSubscriptionDetailsAsync(string subscriptionId);

        /// <summary>
        /// Process refund for a payment
        /// </summary>
        object ProcessRefundAsync(string paymentId, decimal amount, string? reason = null);
    }
}