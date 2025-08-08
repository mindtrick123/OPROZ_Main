using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;
using OPROZ_Main.ViewModels;

namespace OPROZ_Main.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardMetrics> GetDashboardMetricsAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfQuarter = new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1);
            var startOfYear = new DateTime(now.Year, 1, 1);
            var lastMonth = startOfMonth.AddMonths(-1);

            // Active subscriptions (based on payment history with valid end dates)
            var activeSubscriptions = await _context.PaymentHistories
                .Where(p => p.Status == PaymentStatus.Success && 
                           p.SubscriptionEndDate.HasValue && 
                           p.SubscriptionEndDate > now)
                .CountAsync();

            // Revenue calculations
            var monthlyRevenue = await _context.PaymentHistories
                .Where(p => p.Status == PaymentStatus.Success && 
                           p.PaymentDate >= startOfMonth)
                .SumAsync(p => p.FinalAmount);

            var quarterlyRevenue = await _context.PaymentHistories
                .Where(p => p.Status == PaymentStatus.Success && 
                           p.PaymentDate >= startOfQuarter)
                .SumAsync(p => p.FinalAmount);

            var yearlyRevenue = await _context.PaymentHistories
                .Where(p => p.Status == PaymentStatus.Success && 
                           p.PaymentDate >= startOfYear)
                .SumAsync(p => p.FinalAmount);

            // User metrics
            var totalUsers = await _context.Users.CountAsync();
            var newUsersThisMonth = await _context.Users
                .Where(u => u.CreatedAt >= startOfMonth)
                .CountAsync();

            // Payment metrics
            var totalPayments = await _context.PaymentHistories.CountAsync();
            var failedPayments = await _context.PaymentHistories
                .Where(p => p.Status == PaymentStatus.Failed)
                .CountAsync();

            // Churn rate calculation (simplified - users who had subscriptions last month but don't this month)
            var lastMonthActiveUsers = await _context.PaymentHistories
                .Where(p => p.Status == PaymentStatus.Success && 
                           p.SubscriptionEndDate.HasValue && 
                           p.SubscriptionEndDate > lastMonth &&
                           p.PaymentDate < startOfMonth)
                .Select(p => p.UserId)
                .Distinct()
                .CountAsync();

            var thisMonthActiveUsers = await _context.PaymentHistories
                .Where(p => p.Status == PaymentStatus.Success && 
                           p.SubscriptionEndDate.HasValue && 
                           p.SubscriptionEndDate > now)
                .Select(p => p.UserId)
                .Distinct()
                .CountAsync();

            var churnRate = lastMonthActiveUsers > 0 
                ? (decimal)(lastMonthActiveUsers - thisMonthActiveUsers) / lastMonthActiveUsers * 100 
                : 0;

            return new DashboardMetrics
            {
                ActiveSubscriptions = activeSubscriptions,
                MonthlyRevenue = monthlyRevenue,
                QuarterlyRevenue = quarterlyRevenue,
                YearlyRevenue = yearlyRevenue,
                ChurnRate = Math.Max(0, churnRate),
                TotalUsers = totalUsers,
                NewUsersThisMonth = newUsersThisMonth,
                TotalPayments = totalPayments,
                FailedPayments = failedPayments
            };
        }

        public async Task<List<ChartData>> GetRevenueChartDataAsync(int months = 12)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-months);

            var revenueData = await _context.PaymentHistories
                .Where(p => p.Status == PaymentStatus.Success && 
                           p.PaymentDate >= startDate)
                .GroupBy(p => new { 
                    Year = p.PaymentDate.Year, 
                    Month = p.PaymentDate.Month 
                })
                .Select(g => new ChartData
                {
                    Label = $"{g.Key.Year}-{g.Key.Month:00}",
                    Value = g.Sum(p => p.FinalAmount),
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return revenueData;
        }

        public async Task<List<ChartData>> GetSubscriptionChartDataAsync(int months = 12)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-months);

            var subscriptionData = await _context.PaymentHistories
                .Where(p => p.Status == PaymentStatus.Success && 
                           p.PaymentDate >= startDate)
                .GroupBy(p => new { 
                    Year = p.PaymentDate.Year, 
                    Month = p.PaymentDate.Month 
                })
                .Select(g => new ChartData
                {
                    Label = $"{g.Key.Year}-{g.Key.Month:00}",
                    Value = g.Count(),
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return subscriptionData;
        }

        public async Task<List<ServiceUsageData>> GetServiceUsageDataAsync()
        {
            var serviceUsage = await _context.PaymentHistories
                .Where(p => p.Status == PaymentStatus.Success)
                .Include(p => p.SubscriptionPlan)
                    .ThenInclude(sp => sp.Service)
                .GroupBy(p => new { 
                    ServiceName = p.SubscriptionPlan.Service.Name,
                    PlanType = p.SubscriptionPlan.Type.ToString()
                })
                .Select(g => new ServiceUsageData
                {
                    ServiceName = g.Key.ServiceName,
                    PlanType = g.Key.PlanType,
                    SubscriptionCount = g.Count(),
                    Revenue = g.Sum(p => p.FinalAmount)
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            return serviceUsage;
        }

        public async Task<List<RecentActivity>> GetRecentActivitiesAsync(int count = 10)
        {
            var recentSignups = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(count / 2)
                .Select(u => new RecentActivity
                {
                    UserName = $"{u.FirstName} {u.LastName}",
                    Action = "New User Registration",
                    Date = u.CreatedAt,
                    Details = u.Email
                })
                .ToListAsync();

            var recentPayments = await _context.PaymentHistories
                .Include(p => p.User)
                .Include(p => p.SubscriptionPlan)
                .OrderByDescending(p => p.PaymentDate)
                .Take(count / 2)
                .Select(p => new RecentActivity
                {
                    UserName = $"{p.User.FirstName} {p.User.LastName}",
                    Action = p.Status == PaymentStatus.Success ? "Payment Successful" : "Payment Failed",
                    Date = p.PaymentDate,
                    Details = $"{p.SubscriptionPlan.Name} - ${p.FinalAmount}"
                })
                .ToListAsync();

            return recentSignups.Concat(recentPayments)
                .OrderByDescending(x => x.Date)
                .Take(count)
                .ToList();
        }

        public async Task<List<PaymentIssue>> GetPaymentIssuesAsync(int count = 10)
        {
            var paymentIssues = await _context.PaymentHistories
                .Include(p => p.User)
                .Include(p => p.SubscriptionPlan)
                .Where(p => p.Status == PaymentStatus.Failed || p.Status == PaymentStatus.Pending)
                .OrderByDescending(p => p.PaymentDate)
                .Take(count)
                .Select(p => new PaymentIssue
                {
                    UserName = $"{p.User.FirstName} {p.User.LastName}",
                    Email = p.User.Email,
                    Amount = p.FinalAmount,
                    PlanName = p.SubscriptionPlan.Name,
                    PaymentDate = p.PaymentDate,
                    Status = p.Status.ToString(),
                    TransactionId = p.TransactionId
                })
                .ToListAsync();

            return paymentIssues;
        }

        public async Task<List<ExpiringSubscription>> GetExpiringSubscriptionsAsync(int days = 30)
        {
            var expiryDate = DateTime.UtcNow.AddDays(days);
            var now = DateTime.UtcNow;

            var expiringSubscriptions = await _context.PaymentHistories
                .Include(p => p.User)
                .Include(p => p.SubscriptionPlan)
                .Where(p => p.Status == PaymentStatus.Success && 
                           p.SubscriptionEndDate.HasValue && 
                           p.SubscriptionEndDate > now &&
                           p.SubscriptionEndDate <= expiryDate)
                .OrderBy(p => p.SubscriptionEndDate)
                .Select(p => new ExpiringSubscription
                {
                    UserName = $"{p.User.FirstName} {p.User.LastName}",
                    Email = p.User.Email,
                    PlanName = p.SubscriptionPlan.Name,
                    ExpiryDate = p.SubscriptionEndDate!.Value,
                    DaysRemaining = (int)(p.SubscriptionEndDate!.Value - now).TotalDays,
                    Amount = p.FinalAmount
                })
                .ToListAsync();

            return expiringSubscriptions;
        }
    }
}