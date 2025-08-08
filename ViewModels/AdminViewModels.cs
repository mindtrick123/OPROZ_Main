using OPROZ_Main.Models;
using System.ComponentModel.DataAnnotations;

namespace OPROZ_Main.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalServices { get; set; }
        public int ActiveServices { get; set; }
        public int TotalPlans { get; set; }
        public int ActivePlans { get; set; }
        public int TotalOffers { get; set; }
        public int ActiveOffers { get; set; }
        public int TotalUsers { get; set; }
        public List<PaymentHistory> RecentPayments { get; set; } = new();
    }

    public class ServiceFormViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Service Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Short Description")]
        public string? ShortDescription { get; set; }

        [StringLength(200)]
        [Display(Name = "Icon Class")]
        public string? IconClass { get; set; }

        [StringLength(200)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Base Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Base price must be positive")]
        public decimal? BasePrice { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;
    }

    public class SubscriptionPlanFormViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Plan Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be positive")]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Duration")]
        public PlanDuration Duration { get; set; }

        [Required]
        [Display(Name = "Plan Type")]
        public PlanType Type { get; set; }

        [StringLength(2000)]
        [Display(Name = "Features (JSON)")]
        public string? Features { get; set; }

        [Display(Name = "Max Users")]
        [Range(0, int.MaxValue, ErrorMessage = "Max users must be positive")]
        public int MaxUsers { get; set; } = 1;

        [Display(Name = "Max Storage (MB)")]
        [Range(0, long.MaxValue, ErrorMessage = "Max storage must be positive")]
        public long MaxStorage { get; set; } = 1024;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is Popular")]
        public bool IsPopular { get; set; } = false;

        [Display(Name = "Selected Services")]
        public List<int> SelectedServiceIds { get; set; } = new();

        public List<Service> AvailableServices { get; set; } = new();
    }

    public class OfferFormViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Offer Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Offer Code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Offer Type")]
        public OfferType Type { get; set; }

        [Required]
        [Display(Name = "Value")]
        [Range(0, double.MaxValue, ErrorMessage = "Value must be positive")]
        public decimal Value { get; set; }

        [Display(Name = "Service")]
        public int? ServiceId { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);

        [Display(Name = "Max Usage Count")]
        [Range(1, int.MaxValue, ErrorMessage = "Max usage count must be positive")]
        public int? MaxUsageCount { get; set; }

        [Display(Name = "Minimum Order Amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Minimum order amount must be positive")]
        public decimal? MinOrderAmount { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public List<Service> AvailableServices { get; set; } = new();
    }
}