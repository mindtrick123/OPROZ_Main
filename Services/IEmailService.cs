namespace OPROZ_Main.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
        Task SendEmailConfirmationAsync(string toEmail, string confirmationLink);
        Task SendPasswordResetAsync(string toEmail, string resetLink);
        Task SendContactFormEmailAsync(string name, string email, string phone, string subject, string message);
    }
}