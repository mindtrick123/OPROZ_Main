using OPROZ_Main.Data;
using OPROZ_Main.Models;
using System.Text.Json;

namespace OPROZ_Main.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string? userId, string? userName, AuditAction action, string tableName, 
                                  string? recordId, string? description, string? ipAddress = null, 
                                  string? userAgent = null, string? requestUrl = null, string? httpMethod = null,
                                  object? oldValues = null, object? newValues = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    TableName = tableName,
                    RecordId = recordId,
                    Description = description,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    RequestUrl = requestUrl,
                    HttpMethod = httpMethod,
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Log audit errors silently to avoid affecting main operations
                // In production, consider logging to a separate system
            }
        }

        public async Task LogUserActionAsync(string? userId, string? userName, string action, string details, 
                                           string? ipAddress = null)
        {
            await LogAsync(userId, userName, AuditAction.Other, "User", userId, 
                          $"{action}: {details}", ipAddress);
        }
    }
}