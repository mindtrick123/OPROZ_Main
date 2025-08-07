using System.ComponentModel.DataAnnotations;

namespace OPROZ_Main.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? ShortDescription { get; set; }

        [StringLength(200)]
        public string? IconClass { get; set; }

        [StringLength(200)]
        public string? ImageUrl { get; set; }

        public decimal? BasePrice { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<SubscriptionPlan> SubscriptionPlans { get; set; } = new List<SubscriptionPlan>();
        public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();
    }
}