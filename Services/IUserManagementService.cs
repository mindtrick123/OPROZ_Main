using OPROZ_Main.ViewModels;
using OPROZ_Main.Models;

namespace OPROZ_Main.Services
{
    public interface IUserManagementService
    {
        Task<UserManagementViewModel> GetUsersAsync(UserSearchFilter filter, int page = 1, int pageSize = 20);
        Task<UserDetailsViewModel> GetUserDetailsAsync(string userId);
        Task<EditUserViewModel> GetEditUserViewModelAsync(string userId);
        Task<bool> UpdateUserAsync(EditUserViewModel model);
        Task<bool> ResetUserPasswordAsync(string userId, string newPassword);
        Task<bool> SuspendUserAsync(string userId, string reason);
        Task<bool> ReactivateUserAsync(string userId);
        Task<bool> AssignRoleAsync(string userId, string role);
        Task<bool> RemoveRoleAsync(string userId, string role);
        Task<List<string>> GetUserRolesAsync(string userId);
        Task<List<string>> GetAvailableRolesAsync();
        Task<List<CompanyOption>> GetCompaniesAsync();
    }
}