using System.ComponentModel.DataAnnotations;

namespace OPROZ_Main.Models
{
    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed,
        Refunded,
        Cancelled
    }

    public enum PaymentMethod
    {
        CreditCard,
        DebitCard,
        NetBanking,
        UPI,
        Wallet,
        Other
    }

    public class PaymentHistory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string TransactionId { get; set; } = string.Empty;

        [StringLength(100)]
        public string? RazorpayPaymentId { get; set; }

        [StringLength(100)]
        public string? RazorpayOrderId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        public int? CompanyId { get; set; }
        public virtual Company? Company { get; set; }

        public int SubscriptionPlanId { get; set; }
        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        public int? OfferId { get; set; }
        public virtual Offer? Offer { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public decimal? DiscountAmount { get; set; }

        [Required]
        public decimal FinalAmount { get; set; }

        [Required]
        public PaymentStatus Status { get; set; }

        public PaymentMethod? Method { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(2000)]
        public string? ResponseData { get; set; } // JSON response from payment gateway

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}