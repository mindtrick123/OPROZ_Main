using OPROZ_Main.Models;
using System.ComponentModel.DataAnnotations;

namespace OPROZ_Main.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<Service> FeaturedServices { get; set; } = new();
        public List<SubscriptionPlan> PopularPlans { get; set; } = new();
    }

    public class AboutViewModel
    {
        public List<TeamMember> TeamMembers { get; set; } = new();
        public CompanyStats CompanyStats { get; set; } = new();
    }

    public class TeamMember
    {
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string LinkedInUrl { get; set; } = string.Empty;
    }

    public class CompanyStats
    {
        public int YearsInBusiness { get; set; }
        public int ProjectsCompleted { get; set; }
        public int HappyClients { get; set; }
        public int TeamMembers { get; set; }
    }

    public class ServicesViewModel
    {
        public List<Service> Services { get; set; } = new();
        public Dictionary<Service, List<SubscriptionPlan>> SubscriptionPlans { get; set; } = new();
    }

    public class ContactViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;
    }

    public class BlogsViewModel
    {
        public List<BlogPost> BlogPosts { get; set; } = new();
    }

    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int ReadTimeMinutes { get; set; }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}