using System.ComponentModel.DataAnnotations;

namespace OPROZ_Main.Models
{
    public enum AuditAction
    {
        Create,
        Update,
        Delete,
        Login,
        Logout,
        PasswordChange,
        Payment,
        Other
    }

    public class AuditLog
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        [StringLength(200)]
        public string? UserName { get; set; }

        [Required]
        public AuditAction Action { get; set; }

        [Required]
        [StringLength(200)]
        public string TableName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? RecordId { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public string? OldValues { get; set; } // JSON string
        public string? NewValues { get; set; } // JSON string

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? RequestUrl { get; set; }

        [StringLength(10)]
        public string? HttpMethod { get; set; }
    }
}