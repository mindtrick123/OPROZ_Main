using OPROZ_Main.Models;
using System.ComponentModel.DataAnnotations;

namespace OPROZ_Main.ViewModels
{
    // Dashboard ViewModels
    public class AdminDashboardViewModel
    {
        public DashboardMetrics Metrics { get; set; } = new();
        public List<ChartData> RevenueChartData { get; set; } = new();
        public List<ChartData> SubscriptionChartData { get; set; } = new();
        public List<ServiceUsageData> ServiceUsageData { get; set; } = new();
        public List<RecentActivity> RecentActivities { get; set; } = new();
        public List<PaymentIssue> PaymentIssues { get; set; } = new();
        public List<ExpiringSubscription> ExpiringSubscriptions { get; set; } = new();
    }

    public class DashboardMetrics
    {
        public int ActiveSubscriptions { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal QuarterlyRevenue { get; set; }
        public decimal YearlyRevenue { get; set; }
        public decimal ChurnRate { get; set; }
        public int TotalUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int TotalPayments { get; set; }
        public int FailedPayments { get; set; }
    }

    public class ChartData
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
    }

    public class ServiceUsageData
    {
        public string ServiceName { get; set; } = string.Empty;
        public int SubscriptionCount { get; set; }
        public decimal Revenue { get; set; }
        public string PlanType { get; set; } = string.Empty;
    }

    public class RecentActivity
    {
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    public class PaymentIssue
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
    }

    public class ExpiringSubscription
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }
        public decimal Amount { get; set; }
    }

    // Reporting ViewModels
    public class ReportFilterViewModel
    {
        [Display(Name = "Report Type")]
        public ReportType ReportType { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Plan Type")]
        public PlanType? PlanType { get; set; }

        [Display(Name = "User Status")]
        public UserStatus? UserStatus { get; set; }

        [Display(Name = "Payment Status")]
        public PaymentStatus? PaymentStatus { get; set; }

        [Display(Name = "Service")]
        public int? ServiceId { get; set; }

        [Display(Name = "Export Format")]
        public ExportFormat ExportFormat { get; set; } = ExportFormat.CSV;
    }

    public enum ReportType
    {
        Users,
        Subscriptions,
        Payments,
        Services,
        AuditLog
    }

    public enum UserStatus
    {
        All,
        Active,
        Inactive,
        Locked
    }

    public enum ExportFormat
    {
        CSV,
        Excel
    }

    // User Management ViewModels
    public class UserManagementViewModel
    {
        public List<UserManagementItem> Users { get; set; } = new();
        public UserSearchFilter SearchFilter { get; set; } = new();
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 20;
        public int TotalUsers { get; set; }
    }

    public class UserManagementItem
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public List<string> Roles { get; set; } = new();
        public string? CompanyName { get; set; }
        public int SubscriptionCount { get; set; }
        public decimal TotalSpent { get; set; }
        public string CurrentPlan { get; set; } = string.Empty;
        public DateTime? SubscriptionExpiry { get; set; }
    }

    public class UserSearchFilter
    {
        [Display(Name = "Search")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Role")]
        public string? Role { get; set; }

        [Display(Name = "Status")]
        public UserStatus Status { get; set; } = UserStatus.All;

        [Display(Name = "Company")]
        public int? CompanyId { get; set; }

        [Display(Name = "Sort By")]
        public UserSortBy SortBy { get; set; } = UserSortBy.CreatedDate;

        [Display(Name = "Sort Order")]
        public SortOrder SortOrder { get; set; } = SortOrder.Descending;
    }

    public enum UserSortBy
    {
        CreatedDate,
        LastLogin,
        Email,
        Name,
        TotalSpent
    }

    public enum SortOrder
    {
        Ascending,
        Descending
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Company")]
        public int? CompanyId { get; set; }

        [Display(Name = "Roles")]
        public List<string> SelectedRoles { get; set; } = new();

        public List<string> AvailableRoles { get; set; } = new();
        public List<CompanyOption> Companies { get; set; } = new();
    }

    public class CompanyOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UserDetailsViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
        public List<PaymentHistory> PaymentHistory { get; set; } = new();
        public List<SubscriptionHistory> SubscriptionHistory { get; set; } = new();
        public List<AuditLog> RecentAuditLogs { get; set; } = new();
        public UserStatistics Statistics { get; set; } = new();
    }

    public class SubscriptionHistory
    {
        public string PlanName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class UserStatistics
    {
        public decimal TotalSpent { get; set; }
        public int TotalPayments { get; set; }
        public int SuccessfulPayments { get; set; }
        public int FailedPayments { get; set; }
        public int ActiveSubscriptions { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public int LoginCount { get; set; }
    }

    public class AdminResetPasswordViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}