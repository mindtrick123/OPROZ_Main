using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;
using OPROZ_Main.Services;
using OPROZ_Main.ViewModels;
using System.Diagnostics;

namespace OPROZ_Main.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, IEmailService emailService, ILogger<HomeController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeIndexViewModel
            {
                FeaturedServices = await _context.Services
                    .Where(s => s.IsActive && s.IsFeatured)
                    .OrderBy(s => s.DisplayOrder)
                    .Take(6)
                    .ToListAsync(),
                PopularPlans = await _context.SubscriptionPlans
                    .Include(p => p.PlanServices)
                    .ThenInclude(ps => ps.Service)
                    .Where(p => p.IsActive && p.IsPopular)
                    .Take(3)
                    .ToListAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> About()
        {
            var model = new AboutViewModel
            {
                TeamMembers = new List<TeamMember>
                {
                    new TeamMember
                    {
                        Name = "John Doe",
                        Position = "CEO & Founder",
                        Description = "Experienced technology leader with 15+ years in software development and business strategy.",
                        ImageUrl = "/images/team/john-doe.jpg",
                        LinkedInUrl = "https://linkedin.com/in/johndoe"
                    },
                    new TeamMember
                    {
                        Name = "Jane Smith",
                        Position = "CTO",
                        Description = "Technical visionary specializing in cloud architecture and scalable systems.",
                        ImageUrl = "/images/team/jane-smith.jpg",
                        LinkedInUrl = "https://linkedin.com/in/janesmith"
                    },
                    new TeamMember
                    {
                        Name = "Mike Johnson",
                        Position = "Head of Marketing",
                        Description = "Digital marketing expert with proven track record in growth marketing.",
                        ImageUrl = "/images/team/mike-johnson.jpg",
                        LinkedInUrl = "https://linkedin.com/in/mikejohnson"
                    }
                },
                CompanyStats = new CompanyStats
                {
                    YearsInBusiness = 5,
                    ProjectsCompleted = 250,
                    HappyClients = 150,
                    TeamMembers = 25
                }
            };

            return View(model);
        }

        public async Task<IActionResult> Services()
        {
            var model = new ServicesViewModel
            {
                Services = await _context.Services
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .ToListAsync(),
                SubscriptionPlans = new Dictionary<Service, List<SubscriptionPlan>>() // For now, empty dictionary until we update view models
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View(new ContactViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Save to database
                    var helpQuery = new HelpQuery
                    {
                        Name = model.Name,
                        Email = model.Email,
                        Phone = model.Phone,
                        Subject = model.Subject,
                        Message = model.Message,
                        Category = "Contact Form",
                        Status = QueryStatus.Open,
                        Priority = QueryPriority.Medium,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.HelpQueries.Add(helpQuery);
                    await _context.SaveChangesAsync();

                    // Send email to support team
                    await _emailService.SendContactFormEmailAsync(
                        model.Name, 
                        model.Email, 
                        model.Phone ?? string.Empty, 
                        model.Subject, 
                        model.Message
                    );

                    // Send auto-reply to user
                    var autoReplySubject = "Thank you for contacting OPROZ";
                    var autoReplyBody = $@"
                        <html>
                        <body>
                            <h2>Thank you for your inquiry!</h2>
                            <p>Dear {model.Name},</p>
                            <p>We have received your message and will get back to you within 24 hours.</p>
                            <p><strong>Your inquiry:</strong></p>
                            <p><strong>Subject:</strong> {model.Subject}</p>
                            <p><strong>Message:</strong> {model.Message}</p>
                            <p>Best regards,<br>OPROZ Support Team</p>
                        </body>
                        </html>";

                    await _emailService.SendEmailAsync(model.Email, autoReplySubject, autoReplyBody);

                    TempData["Success"] = "Thank you for your message! We'll get back to you soon.";
                    return RedirectToAction(nameof(Contact));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing contact form submission from {Email}", model.Email);
                    TempData["Error"] = "There was an error sending your message. Please try again.";
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Blogs()
        {
            var model = new BlogsViewModel
            {
                BlogPosts = new List<BlogPost>
                {
                    new BlogPost
                    {
                        Id = 1,
                        Title = "The Future of SaaS: Trends to Watch in 2024",
                        Summary = "Explore the latest trends shaping the Software as a Service industry and what they mean for businesses.",
                        Author = "John Doe",
                        PublishedDate = DateTime.Now.AddDays(-5),
                        ImageUrl = "/images/blog/saas-trends-2024.jpg",
                        Category = "Technology",
                        ReadTimeMinutes = 8
                    },
                    new BlogPost
                    {
                        Id = 2,
                        Title = "Digital Transformation: A Complete Guide for Small Businesses",
                        Summary = "Learn how small businesses can successfully navigate digital transformation with practical strategies.",
                        Author = "Jane Smith",
                        PublishedDate = DateTime.Now.AddDays(-12),
                        ImageUrl = "/images/blog/digital-transformation.jpg",
                        Category = "Business",
                        ReadTimeMinutes = 12
                    },
                    new BlogPost
                    {
                        Id = 3,
                        Title = "Cloud Security Best Practices Every Business Should Know",
                        Summary = "Essential security measures to protect your business data in the cloud.",
                        Author = "Mike Johnson",
                        PublishedDate = DateTime.Now.AddDays(-20),
                        ImageUrl = "/images/blog/cloud-security.jpg",
                        Category = "Security",
                        ReadTimeMinutes = 10
                    },
                    new BlogPost
                    {
                        Id = 4,
                        Title = "How to Choose the Right Technology Stack for Your Startup",
                        Summary = "A comprehensive guide to selecting technologies that will scale with your business.",
                        Author = "John Doe",
                        PublishedDate = DateTime.Now.AddDays(-28),
                        ImageUrl = "/images/blog/tech-stack.jpg",
                        Category = "Development",
                        ReadTimeMinutes = 15
                    }
                }
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}