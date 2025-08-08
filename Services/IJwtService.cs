using System.Security.Claims;

namespace OPROZ_Main.Services
{
    public interface IJwtService
    {
        string GenerateToken(string userId, string email, string role, int? companyId = null);
        ClaimsPrincipal? ValidateToken(string token);
        string? GetUserIdFromToken(string token);
        string? GetEmailFromToken(string token);
        string? GetRoleFromToken(string token);
    }
}