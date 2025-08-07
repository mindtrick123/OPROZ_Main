using Microsoft.AspNetCore.SignalR;

namespace OPROZ_Main.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendNotificationAsync(string userId, string title, string message)
        {
            try
            {
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
                {
                    Title = title,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            }
        }

        public async Task SendBroadcastNotificationAsync(string title, string message)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = title,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Broadcast notification sent: {Title}", title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending broadcast notification");
            }
        }

        public async Task SendNotificationToRoleAsync(string role, string title, string message)
        {
            try
            {
                await _hubContext.Clients.Group(role).SendAsync("ReceiveNotification", new
                {
                    Title = title,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Notification sent to role {Role}: {Title}", role, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to role {Role}", role);
            }
        }
    }

    public class NotificationHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
}