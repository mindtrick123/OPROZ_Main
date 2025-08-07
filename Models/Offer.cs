using System.ComponentModel.DataAnnotations;

namespace OPROZ_Main.Models
{
    public enum OfferType
    {
        Percentage,
        FixedAmount,
        FreeMonth
    }

    public class Offer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        public OfferType Type { get; set; }

        public decimal Value { get; set; } // Percentage or fixed amount

        public int? ServiceId { get; set; }
        public virtual Service? Service { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int? MaxUsageCount { get; set; }
        public int UsedCount { get; set; } = 0;

        public decimal? MinOrderAmount { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();
    }
}