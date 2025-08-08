using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OPROZ_Main.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        public int? CompanyId { get; set; }
        public virtual Company? Company { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Email verification - using new to hide inherited EmailConfirmed
        public new bool EmailConfirmed { get; set; } = false;
        public DateTime? EmailConfirmedAt { get; set; }

        public virtual ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();
        public virtual ICollection<HelpQuery> HelpQueries { get; set; } = new List<HelpQuery>();
    }
}