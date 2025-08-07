using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OPROZ_Main.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

// ApplicationUser extending IdentityUser
public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

// Company model for multi-tenancy
public class Company
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? Website { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();
}

// Service model
public class Service
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();
}

// SubscriptionPlan model
public class SubscriptionPlan
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public int DurationInDays { get; set; }

    [StringLength(50)]
    public string BillingCycle { get; set; } = "Monthly"; // Monthly, Yearly, etc.

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();
}

// Offer model
public class Offer
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? DiscountPercentage { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int? ServiceId { get; set; }
    public Service? Service { get; set; }

    public int? SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();
}

// PaymentHistory model
public class PaymentHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Success, Failed, Refunded

    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty; // Card, UPI, NetBanking, etc.

    [StringLength(100)]
    public string? RazorpayPaymentId { get; set; }

    [StringLength(100)]
    public string? RazorpayOrderId { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public int? OfferId { get; set; }
    public Offer? Offer { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Notes { get; set; }
}

// HelpQuery model
public class HelpQuery
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Open"; // Open, In Progress, Resolved, Closed

    [StringLength(50)]
    public string Category { get; set; } = "General"; // General, Technical, Billing, etc.

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    [StringLength(2000)]
    public string? Response { get; set; }

    [StringLength(100)]
    public string? AssignedTo { get; set; }
}

// AuditLog model
public class AuditLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Login, Logout

    [Required]
    [StringLength(100)]
    public string TableName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? RecordId { get; set; }

    [StringLength(2000)]
    public string? OldValues { get; set; }

    [StringLength(2000)]
    public string? NewValues { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Notes { get; set; }
}
