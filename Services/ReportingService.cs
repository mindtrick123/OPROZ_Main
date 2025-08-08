using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OPROZ_Main.Data;
using OPROZ_Main.Models;
using OPROZ_Main.ViewModels;
using System.Text;

namespace OPROZ_Main.Services
{
    public class ReportingService : IReportingService
    {
        private readonly ApplicationDbContext _context;

        public ReportingService(ApplicationDbContext context)
        {
            _context = context;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<byte[]> GenerateUserReportAsync(ReportFilterViewModel filter)
        {
            var query = _context.Users.AsQueryable();

            // Apply filters
            if (filter.StartDate.HasValue)
                query = query.Where(u => u.CreatedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(u => u.CreatedAt <= filter.EndDate.Value);

            if (filter.UserStatus.HasValue && filter.UserStatus != UserStatus.All)
            {
                switch (filter.UserStatus.Value)
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

            var users = await query
                .Include(u => u.Company)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            if (filter.ExportFormat == ExportFormat.CSV)
                return GenerateUserCsv(users);
            else
                return GenerateUserExcel(users);
        }

        public async Task<byte[]> GenerateSubscriptionReportAsync(ReportFilterViewModel filter)
        {
            var query = _context.PaymentHistories
                .Include(p => p.User)
                .Include(p => p.SubscriptionPlan)
                    .ThenInclude(sp => sp.Service)
                .Include(p => p.Company)
                .AsQueryable();

            // Apply filters
            if (filter.StartDate.HasValue)
                query = query.Where(p => p.PaymentDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(p => p.PaymentDate <= filter.EndDate.Value);

            if (filter.PlanType.HasValue)
                query = query.Where(p => p.SubscriptionPlan.Type == filter.PlanType.Value);

            if (filter.ServiceId.HasValue)
                query = query.Where(p => p.SubscriptionPlan.ServiceId == filter.ServiceId.Value);

            var subscriptions = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            if (filter.ExportFormat == ExportFormat.CSV)
                return GenerateSubscriptionCsv(subscriptions);
            else
                return GenerateSubscriptionExcel(subscriptions);
        }

        public async Task<byte[]> GeneratePaymentReportAsync(ReportFilterViewModel filter)
        {
            var query = _context.PaymentHistories
                .Include(p => p.User)
                .Include(p => p.SubscriptionPlan)
                    .ThenInclude(sp => sp.Service)
                .Include(p => p.Company)
                .AsQueryable();

            // Apply filters
            if (filter.StartDate.HasValue)
                query = query.Where(p => p.PaymentDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(p => p.PaymentDate <= filter.EndDate.Value);

            if (filter.PaymentStatus.HasValue)
                query = query.Where(p => p.Status == filter.PaymentStatus.Value);

            if (filter.ServiceId.HasValue)
                query = query.Where(p => p.SubscriptionPlan.ServiceId == filter.ServiceId.Value);

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            if (filter.ExportFormat == ExportFormat.CSV)
                return GeneratePaymentCsv(payments);
            else
                return GeneratePaymentExcel(payments);
        }

        public async Task<byte[]> GenerateServiceReportAsync(ReportFilterViewModel filter)
        {
            var services = await _context.Services
                .Include(s => s.SubscriptionPlans)
                    .ThenInclude(sp => sp.PaymentHistories)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            if (filter.ExportFormat == ExportFormat.CSV)
                return GenerateServiceCsv(services);
            else
                return GenerateServiceExcel(services);
        }

        public async Task<byte[]> GenerateAuditLogReportAsync(ReportFilterViewModel filter)
        {
            var query = _context.AuditLogs.AsQueryable();

            // Apply filters
            if (filter.StartDate.HasValue)
                query = query.Where(a => a.CreatedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(a => a.CreatedAt <= filter.EndDate.Value);

            var auditLogs = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            if (filter.ExportFormat == ExportFormat.CSV)
                return GenerateAuditLogCsv(auditLogs);
            else
                return GenerateAuditLogExcel(auditLogs);
        }

        #region CSV Generation Methods

        private byte[] GenerateUserCsv(List<ApplicationUser> users)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Id,First Name,Last Name,Email,Phone,Company,Active,Email Confirmed,Created At,Last Login");

            foreach (var user in users)
            {
                csv.AppendLine($"{EscapeCsv(user.Id)},{EscapeCsv(user.FirstName)},{EscapeCsv(user.LastName)}," +
                              $"{EscapeCsv(user.Email)},{EscapeCsv(user.PhoneNumber ?? "")}," +
                              $"{EscapeCsv(user.Company?.Name ?? "")},{user.IsActive},{user.EmailConfirmed}," +
                              $"{user.CreatedAt:yyyy-MM-dd HH:mm:ss},{user.LastLoginAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GenerateSubscriptionCsv(List<PaymentHistory> subscriptions)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Transaction ID,User Name,Email,Service,Plan,Plan Type,Amount,Start Date,End Date,Status,Payment Date");

            foreach (var sub in subscriptions)
            {
                csv.AppendLine($"{EscapeCsv(sub.TransactionId)},{EscapeCsv($"{sub.User.FirstName} {sub.User.LastName}")}," +
                              $"{EscapeCsv(sub.User.Email)},{EscapeCsv(sub.SubscriptionPlan.Service.Name)}," +
                              $"{EscapeCsv(sub.SubscriptionPlan.Name)},{sub.SubscriptionPlan.Type},{sub.FinalAmount}," +
                              $"{sub.SubscriptionStartDate?.ToString("yyyy-MM-dd") ?? ""}," +
                              $"{sub.SubscriptionEndDate?.ToString("yyyy-MM-dd") ?? ""},{sub.Status}," +
                              $"{sub.PaymentDate:yyyy-MM-dd HH:mm:ss}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GeneratePaymentCsv(List<PaymentHistory> payments)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Transaction ID,Razorpay Payment ID,User Name,Email,Service,Plan,Amount,Discount,Final Amount,Status,Method,Payment Date");

            foreach (var payment in payments)
            {
                csv.AppendLine($"{EscapeCsv(payment.TransactionId)},{EscapeCsv(payment.RazorpayPaymentId ?? "")}," +
                              $"{EscapeCsv($"{payment.User.FirstName} {payment.User.LastName}")}," +
                              $"{EscapeCsv(payment.User.Email)},{EscapeCsv(payment.SubscriptionPlan.Service.Name)}," +
                              $"{EscapeCsv(payment.SubscriptionPlan.Name)},{payment.Amount},{payment.DiscountAmount ?? 0}," +
                              $"{payment.FinalAmount},{payment.Status},{payment.Method?.ToString() ?? ""}," +
                              $"{payment.PaymentDate:yyyy-MM-dd HH:mm:ss}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GenerateServiceCsv(List<Service> services)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID,Name,Description,Base Price,Active,Featured,Total Subscriptions,Total Revenue");

            foreach (var service in services)
            {
                var totalSubscriptions = service.SubscriptionPlans.Sum(sp => sp.PaymentHistories.Count(ph => ph.Status == PaymentStatus.Success));
                var totalRevenue = service.SubscriptionPlans.Sum(sp => sp.PaymentHistories.Where(ph => ph.Status == PaymentStatus.Success).Sum(ph => ph.FinalAmount));

                csv.AppendLine($"{service.Id},{EscapeCsv(service.Name)},{EscapeCsv(service.Description ?? "")}," +
                              $"{service.BasePrice ?? 0},{service.IsActive},{service.IsFeatured}," +
                              $"{totalSubscriptions},{totalRevenue}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GenerateAuditLogCsv(List<AuditLog> auditLogs)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID,User ID,User Name,Action,Table Name,Record ID,Description,IP Address,Created At");

            foreach (var log in auditLogs)
            {
                csv.AppendLine($"{log.Id},{EscapeCsv(log.UserId ?? "")},{EscapeCsv(log.UserName ?? "")}," +
                              $"{log.Action},{EscapeCsv(log.TableName)},{EscapeCsv(log.RecordId ?? "")}," +
                              $"{EscapeCsv(log.Description ?? "")},{EscapeCsv(log.IpAddress ?? "")}," +
                              $"{log.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #endregion

        #region Excel Generation Methods

        private byte[] GenerateUserExcel(List<ApplicationUser> users)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Users");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "First Name";
            worksheet.Cells[1, 3].Value = "Last Name";
            worksheet.Cells[1, 4].Value = "Email";
            worksheet.Cells[1, 5].Value = "Phone";
            worksheet.Cells[1, 6].Value = "Company";
            worksheet.Cells[1, 7].Value = "Active";
            worksheet.Cells[1, 8].Value = "Email Confirmed";
            worksheet.Cells[1, 9].Value = "Created At";
            worksheet.Cells[1, 10].Value = "Last Login";

            // Data
            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                var row = i + 2;
                
                worksheet.Cells[row, 1].Value = user.Id;
                worksheet.Cells[row, 2].Value = user.FirstName;
                worksheet.Cells[row, 3].Value = user.LastName;
                worksheet.Cells[row, 4].Value = user.Email;
                worksheet.Cells[row, 5].Value = user.PhoneNumber;
                worksheet.Cells[row, 6].Value = user.Company?.Name;
                worksheet.Cells[row, 7].Value = user.IsActive;
                worksheet.Cells[row, 8].Value = user.EmailConfirmed;
                worksheet.Cells[row, 9].Value = user.CreatedAt;
                worksheet.Cells[row, 10].Value = user.LastLoginAt;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }

        private byte[] GenerateSubscriptionExcel(List<PaymentHistory> subscriptions)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Subscriptions");

            // Headers
            worksheet.Cells[1, 1].Value = "Transaction ID";
            worksheet.Cells[1, 2].Value = "User Name";
            worksheet.Cells[1, 3].Value = "Email";
            worksheet.Cells[1, 4].Value = "Service";
            worksheet.Cells[1, 5].Value = "Plan";
            worksheet.Cells[1, 6].Value = "Plan Type";
            worksheet.Cells[1, 7].Value = "Amount";
            worksheet.Cells[1, 8].Value = "Start Date";
            worksheet.Cells[1, 9].Value = "End Date";
            worksheet.Cells[1, 10].Value = "Status";
            worksheet.Cells[1, 11].Value = "Payment Date";

            // Data
            for (int i = 0; i < subscriptions.Count; i++)
            {
                var sub = subscriptions[i];
                var row = i + 2;
                
                worksheet.Cells[row, 1].Value = sub.TransactionId;
                worksheet.Cells[row, 2].Value = $"{sub.User.FirstName} {sub.User.LastName}";
                worksheet.Cells[row, 3].Value = sub.User.Email;
                worksheet.Cells[row, 4].Value = sub.SubscriptionPlan.Service.Name;
                worksheet.Cells[row, 5].Value = sub.SubscriptionPlan.Name;
                worksheet.Cells[row, 6].Value = sub.SubscriptionPlan.Type.ToString();
                worksheet.Cells[row, 7].Value = sub.FinalAmount;
                worksheet.Cells[row, 8].Value = sub.SubscriptionStartDate;
                worksheet.Cells[row, 9].Value = sub.SubscriptionEndDate;
                worksheet.Cells[row, 10].Value = sub.Status.ToString();
                worksheet.Cells[row, 11].Value = sub.PaymentDate;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        private byte[] GeneratePaymentExcel(List<PaymentHistory> payments)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Payments");

            // Headers
            worksheet.Cells[1, 1].Value = "Transaction ID";
            worksheet.Cells[1, 2].Value = "Razorpay Payment ID";
            worksheet.Cells[1, 3].Value = "User Name";
            worksheet.Cells[1, 4].Value = "Email";
            worksheet.Cells[1, 5].Value = "Service";
            worksheet.Cells[1, 6].Value = "Plan";
            worksheet.Cells[1, 7].Value = "Amount";
            worksheet.Cells[1, 8].Value = "Discount";
            worksheet.Cells[1, 9].Value = "Final Amount";
            worksheet.Cells[1, 10].Value = "Status";
            worksheet.Cells[1, 11].Value = "Method";
            worksheet.Cells[1, 12].Value = "Payment Date";

            // Data
            for (int i = 0; i < payments.Count; i++)
            {
                var payment = payments[i];
                var row = i + 2;
                
                worksheet.Cells[row, 1].Value = payment.TransactionId;
                worksheet.Cells[row, 2].Value = payment.RazorpayPaymentId;
                worksheet.Cells[row, 3].Value = $"{payment.User.FirstName} {payment.User.LastName}";
                worksheet.Cells[row, 4].Value = payment.User.Email;
                worksheet.Cells[row, 5].Value = payment.SubscriptionPlan.Service.Name;
                worksheet.Cells[row, 6].Value = payment.SubscriptionPlan.Name;
                worksheet.Cells[row, 7].Value = payment.Amount;
                worksheet.Cells[row, 8].Value = payment.DiscountAmount;
                worksheet.Cells[row, 9].Value = payment.FinalAmount;
                worksheet.Cells[row, 10].Value = payment.Status.ToString();
                worksheet.Cells[row, 11].Value = payment.Method?.ToString();
                worksheet.Cells[row, 12].Value = payment.PaymentDate;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        private byte[] GenerateServiceExcel(List<Service> services)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Services");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Base Price";
            worksheet.Cells[1, 5].Value = "Active";
            worksheet.Cells[1, 6].Value = "Featured";
            worksheet.Cells[1, 7].Value = "Total Subscriptions";
            worksheet.Cells[1, 8].Value = "Total Revenue";

            // Data
            for (int i = 0; i < services.Count; i++)
            {
                var service = services[i];
                var row = i + 2;
                var totalSubscriptions = service.SubscriptionPlans.Sum(sp => sp.PaymentHistories.Count(ph => ph.Status == PaymentStatus.Success));
                var totalRevenue = service.SubscriptionPlans.Sum(sp => sp.PaymentHistories.Where(ph => ph.Status == PaymentStatus.Success).Sum(ph => ph.FinalAmount));
                
                worksheet.Cells[row, 1].Value = service.Id;
                worksheet.Cells[row, 2].Value = service.Name;
                worksheet.Cells[row, 3].Value = service.Description;
                worksheet.Cells[row, 4].Value = service.BasePrice;
                worksheet.Cells[row, 5].Value = service.IsActive;
                worksheet.Cells[row, 6].Value = service.IsFeatured;
                worksheet.Cells[row, 7].Value = totalSubscriptions;
                worksheet.Cells[row, 8].Value = totalRevenue;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        private byte[] GenerateAuditLogExcel(List<AuditLog> auditLogs)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Audit Logs");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "User ID";
            worksheet.Cells[1, 3].Value = "User Name";
            worksheet.Cells[1, 4].Value = "Action";
            worksheet.Cells[1, 5].Value = "Table Name";
            worksheet.Cells[1, 6].Value = "Record ID";
            worksheet.Cells[1, 7].Value = "Description";
            worksheet.Cells[1, 8].Value = "IP Address";
            worksheet.Cells[1, 9].Value = "Created At";

            // Data
            for (int i = 0; i < auditLogs.Count; i++)
            {
                var log = auditLogs[i];
                var row = i + 2;
                
                worksheet.Cells[row, 1].Value = log.Id;
                worksheet.Cells[row, 2].Value = log.UserId;
                worksheet.Cells[row, 3].Value = log.UserName;
                worksheet.Cells[row, 4].Value = log.Action.ToString();
                worksheet.Cells[row, 5].Value = log.TableName;
                worksheet.Cells[row, 6].Value = log.RecordId;
                worksheet.Cells[row, 7].Value = log.Description;
                worksheet.Cells[row, 8].Value = log.IpAddress;
                worksheet.Cells[row, 9].Value = log.CreatedAt;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        #endregion

        #region Helper Methods

        private string EscapeCsv(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            if (input.Contains(",") || input.Contains("\"") || input.Contains("\n"))
                return $"\"{input.Replace("\"", "\"\"")}\"";

            return input;
        }

        public string GetContentType(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.CSV => "text/csv",
                ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }

        public string GetFileExtension(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.CSV => ".csv",
                ExportFormat.Excel => ".xlsx",
                _ => ".txt"
            };
        }

        public string GetFileName(ReportType reportType, ExportFormat format)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var extension = GetFileExtension(format);
            return $"{reportType}_Report_{timestamp}{extension}";
        }

        #endregion
    }
}