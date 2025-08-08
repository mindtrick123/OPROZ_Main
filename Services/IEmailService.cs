namespace OPROZ_Main.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
        Task SendEmailConfirmationAsync(string toEmail, string confirmationLink);
        Task SendPasswordResetAsync(string toEmail, string resetLink);
        Task SendContactFormEmailAsync(string name, string email, string phone, string subject, string message);
        Task SendPaymentSuccessEmailAsync(string toEmail, string userName, string planName, decimal amount, string transactionId, DateTime subscriptionEndDate);
        Task SendPaymentFailureEmailAsync(string toEmail, string userName, string planName, decimal amount, string transactionId, string reason = "");
        Task SendPaymentPendingEmailAsync(string toEmail, string userName, string planName, decimal amount, string transactionId);
    }
}