using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Data;
using OPROZ_Main.Models;
using OPROZ_Main.ViewModels;

namespace OPROZ_Main.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagementService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<UserManagementViewModel> GetUsersAsync(UserSearchFilter filter, int page = 1, int pageSize = 20)
        {
            var query = _context.Users
                .Include(u => u.Company)
                .AsQueryable();

            // Apply search term filter
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(u => 
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)));
            }

            // Apply status filter
            if (filter.Status != UserStatus.All)
            {
                switch (filter.Status)
                {
                    case UserStatus.Active:
                        query = query.Where(u => u.IsActive);
                        break;
                    case UserStatus.Inactive:
                        query = query.Where(u => !u.IsActive);
                        break;
                    case UserStatus.Locked:
                        query = query.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTime.UtcNow);
                        break;
                }
            }

            // Apply company filter
            if (filter.CompanyId.HasValue)
            {
                query = query.Where(u => u.CompanyId == filter.CompanyId.Value);
            }

            // Apply sorting
            query = filter.SortBy switch
            {
                UserSortBy.CreatedDate => filter.SortOrder == SortOrder.Ascending 
                    ? query.OrderBy(u => u.CreatedAt) 
                    : query.OrderByDescending(u => u.CreatedAt),
                UserSortBy.LastLogin => filter.SortOrder == SortOrder.Ascending 
                    ? query.OrderBy(u => u.LastLoginAt) 
                    : query.OrderByDescending(u => u.LastLoginAt),
                UserSortBy.Email => filter.SortOrder == SortOrder.Ascending 
                    ? query.OrderBy(u => u.Email) 
                    : query.OrderByDescending(u => u.Email),
                UserSortBy.Name => filter.SortOrder == SortOrder.Ascending 
                    ? query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName) 
                    : query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };

            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userItems = new List<UserManagementItem>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                
                // Get subscription info
                var latestPayment = await _context.PaymentHistories
                    .Where(p => p.UserId == user.Id && p.Status == PaymentStatus.Success)
                    .Include(p => p.SubscriptionPlan)
                    .OrderByDescending(p => p.PaymentDate)
                    .FirstOrDefaultAsync();

                var subscriptionCount = await _context.PaymentHistories
                    .Where(p => p.UserId == user.Id && p.Status == PaymentStatus.Success)
                    .CountAsync();

                var totalSpent = await _context.PaymentHistories
                    .Where(p => p.UserId == user.Id && p.Status == PaymentStatus.Success)
                    .SumAsync(p => p.FinalAmount);

                userItems.Add(new UserManagementItem
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? "",
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    Roles = roles.ToList(),
                    CompanyName = user.Company?.Name,
                    SubscriptionCount = subscriptionCount,
                    TotalSpent = totalSpent,
                    CurrentPlan = latestPayment?.SubscriptionPlan.Name ?? "No Active Plan",
                    SubscriptionExpiry = latestPayment?.SubscriptionEndDate
                });
            }

            return new UserManagementViewModel
            {
                Users = userItems,
                SearchFilter = filter,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                TotalUsers = totalUsers
            };
        }

        public async Task<UserDetailsViewModel> GetUserDetailsAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new ArgumentException("User not found");

            var roles = await _userManager.GetRolesAsync(user);

            var paymentHistory = await _context.PaymentHistories
                .Where(p => p.UserId == userId)
                .Include(p => p.SubscriptionPlan)
                    .ThenInclude(sp => sp.Service)
                .OrderByDescending(p => p.PaymentDate)
                .Take(20)
                .ToListAsync();

            var subscriptionHistory = paymentHistory
                .Where(p => p.Status == PaymentStatus.Success)
                .Select(p => new SubscriptionHistory
                {
                    PlanName = p.SubscriptionPlan.Name,
                    ServiceName = p.SubscriptionPlan.Service.Name,
                    StartDate = p.SubscriptionStartDate ?? p.PaymentDate,
                    EndDate = p.SubscriptionEndDate ?? p.PaymentDate.AddMonths(1),
                    Amount = p.FinalAmount,
                    Status = p.SubscriptionEndDate > DateTime.UtcNow ? "Active" : "Expired",
                    IsActive = p.SubscriptionEndDate > DateTime.UtcNow
                })
                .ToList();

            var auditLogs = await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(20)
                .ToListAsync();

            var statistics = new UserStatistics
            {
                TotalSpent = paymentHistory.Where(p => p.Status == PaymentStatus.Success).Sum(p => p.FinalAmount),
                TotalPayments = paymentHistory.Count,
                SuccessfulPayments = paymentHistory.Count(p => p.Status == PaymentStatus.Success),
                FailedPayments = paymentHistory.Count(p => p.Status == PaymentStatus.Failed),
                ActiveSubscriptions = subscriptionHistory.Count(s => s.IsActive),
                LastLoginDate = user.LastLoginAt,
                LoginCount = auditLogs.Count(a => a.Action == AuditAction.Login)
            };

            return new UserDetailsViewModel
            {
                User = user,
                Roles = roles.ToList(),
                PaymentHistory = paymentHistory,
                SubscriptionHistory = subscriptionHistory,
                RecentAuditLogs = auditLogs,
                Statistics = statistics
            };
        }

        public async Task<EditUserViewModel> GetEditUserViewModelAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var availableRoles = await GetAvailableRolesAsync();
            var companies = await GetCompaniesAsync();

            return new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                CompanyId = user.CompanyId,
                SelectedRoles = roles.ToList(),
                AvailableRoles = availableRoles,
                Companies = companies
            };
        }

        public async Task<bool> UpdateUserAsync(EditUserViewModel model)
        {
            var user = await _context.Users.FindAsync(model.Id);
            if (user == null)
                return false;

            try
            {
                // Update user properties
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.NormalizedEmail = model.Email.ToUpper();
                user.NormalizedUserName = model.Email.ToUpper();
                user.PhoneNumber = model.PhoneNumber;
                user.IsActive = model.IsActive;
                user.EmailConfirmed = model.EmailConfirmed;
                user.CompanyId = model.CompanyId;

                await _context.SaveChangesAsync();

                // Update roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                var rolesToRemove = currentRoles.Except(model.SelectedRoles);
                var rolesToAdd = model.SelectedRoles.Except(currentRoles);

                if (rolesToRemove.Any())
                    await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

                if (rolesToAdd.Any())
                    await _userManager.AddToRolesAsync(user, rolesToAdd);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ResetUserPasswordAsync(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SuspendUserAsync(string userId, string reason)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            try
            {
                user.IsActive = false;
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100); // Effectively permanent
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ReactivateUserAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            try
            {
                user.IsActive = true;
                user.LockoutEnd = null;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AssignRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            try
            {
                var result = await _userManager.AddToRoleAsync(user, role);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            try
            {
                var result = await _userManager.RemoveFromRoleAsync(user, role);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new List<string>();

            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public async Task<List<string>> GetAvailableRolesAsync()
        {
            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return roles.Where(r => r != null).Select(r => r!).ToList();
        }

        public async Task<List<CompanyOption>> GetCompaniesAsync()
        {
            var companies = await _context.Companies
                .Where(c => c.IsActive)
                .Select(c => new CompanyOption
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return companies;
        }
    }
}