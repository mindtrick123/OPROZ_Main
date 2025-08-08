using OPROZ_Main.ViewModels;

namespace OPROZ_Main.Services
{
    public interface IReportingService
    {
        Task<byte[]> GenerateUserReportAsync(ReportFilterViewModel filter);
        Task<byte[]> GenerateSubscriptionReportAsync(ReportFilterViewModel filter);
        Task<byte[]> GeneratePaymentReportAsync(ReportFilterViewModel filter);
        Task<byte[]> GenerateServiceReportAsync(ReportFilterViewModel filter);
        Task<byte[]> GenerateAuditLogReportAsync(ReportFilterViewModel filter);
        string GetContentType(ExportFormat format);
        string GetFileExtension(ExportFormat format);
        string GetFileName(ReportType reportType, ExportFormat format);
    }
}