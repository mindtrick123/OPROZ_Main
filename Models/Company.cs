using System.ComponentModel.DataAnnotations;

namespace OPROZ_Main.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();
    }
}