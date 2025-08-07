namespace OPROZ_Main.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string userId, string title, string message);
        Task SendBroadcastNotificationAsync(string title, string message);
        Task SendNotificationToRoleAsync(string role, string title, string message);
    }
}