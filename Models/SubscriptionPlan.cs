using System.ComponentModel.DataAnnotations;

namespace OPROZ_Main.Models
{
    public enum PlanType
    {
        Basic,
        Standard,
        Premium,
        Enterprise
    }

    public enum PlanDuration
    {
        Monthly,
        Quarterly,
        Yearly
    }

    public class SubscriptionPlan
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public PlanDuration Duration { get; set; }

        [Required]
        public PlanType Type { get; set; }

        [StringLength(2000)]
        public string? Features { get; set; } // JSON string of features

        public int MaxUsers { get; set; } = 1;
        public long MaxStorage { get; set; } = 1024; // in MB

        public bool IsActive { get; set; } = true;
        public bool IsPopular { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();
        public virtual ICollection<PlanService> PlanServices { get; set; } = new List<PlanService>();
    }
}