using OPROZ_Main.Models;

namespace OPROZ_Main.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(string? userId, string? userName, AuditAction action, string tableName, 
                     string? recordId, string? description, string? ipAddress = null, 
                     string? userAgent = null, string? requestUrl = null, string? httpMethod = null,
                     object? oldValues = null, object? newValues = null);
        Task LogUserActionAsync(string? userId, string? userName, string action, string details, 
                               string? ipAddress = null);
    }
}