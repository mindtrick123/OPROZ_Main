using System.ComponentModel.DataAnnotations;

namespace OPROZ_Main.Models
{
    public enum QueryStatus
    {
        Pending,
        Open,
        InProgress,
        Resolved,
        Closed
    }

    public enum QueryPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class HelpQuery
    {
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
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public QueryStatus Status { get; set; } = QueryStatus.Pending;
        public QueryPriority Priority { get; set; } = QueryPriority.Medium;

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(2000)]
        public string? AdminResponse { get; set; }

        public string? AssignedToUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}