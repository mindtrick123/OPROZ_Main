using OPROZ_Main.ViewModels;

namespace OPROZ_Main.Services
{
    public interface IAnalyticsService
    {
        Task<DashboardMetrics> GetDashboardMetricsAsync();
        Task<List<ChartData>> GetRevenueChartDataAsync(int months = 12);
        Task<List<ChartData>> GetSubscriptionChartDataAsync(int months = 12);
        Task<List<ServiceUsageData>> GetServiceUsageDataAsync();
        Task<List<RecentActivity>> GetRecentActivitiesAsync(int count = 10);
        Task<List<PaymentIssue>> GetPaymentIssuesAsync(int count = 10);
        Task<List<ExpiringSubscription>> GetExpiringSubscriptionsAsync(int days = 30);
    }
}