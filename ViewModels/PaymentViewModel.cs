using System.ComponentModel.DataAnnotations;
using OPROZ_Main.Models;

namespace OPROZ_Main.ViewModels
{
    public class PaymentViewModel
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string KeyId { get; set; } = string.Empty;
        public int SubscriptionPlanId { get; set; }
        public int? OfferId { get; set; }
    }

    public class PaymentCallbackViewModel
    {
        [Required]
        public string RazorpayOrderId { get; set; } = string.Empty;

        [Required]
        public string RazorpayPaymentId { get; set; } = string.Empty;

        [Required]
        public string RazorpaySignature { get; set; } = string.Empty;

        public int SubscriptionPlanId { get; set; }
        public int? OfferId { get; set; }
    }

    public class SubscriptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public PlanDuration Duration { get; set; }
        public PlanType Type { get; set; }
        public string Features { get; set; } = string.Empty;
        public int MaxUsers { get; set; }
        public long MaxStorage { get; set; }
        public bool IsPopular { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        
        // Parsed features for display
        public List<string> FeatureList { get; set; } = new List<string>();
    }

    public class PaymentHistoryViewModel
    {
        public int Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string? RazorpayPaymentId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public PaymentStatus Status { get; set; }
        public PaymentMethod? Method { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public string? Notes { get; set; }
    }
}