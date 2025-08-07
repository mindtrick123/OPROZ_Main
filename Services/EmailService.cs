using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace OPROZ_Main.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var message = new MimeMessage();

                message.From.Add(new MailboxAddress(
                    emailSettings["FromName"], 
                    emailSettings["FromEmail"]
                ));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                message.Body = new TextPart(isHtml ? TextFormat.Html : TextFormat.Plain)
                {
                    Text = body
                };

                using var client = new SmtpClient();
                
                await client.ConnectAsync(
                    emailSettings["SmtpServer"], 
                    int.Parse(emailSettings["SmtpPort"]!), 
                    bool.Parse(emailSettings["EnableSsl"]!)
                );

                await client.AuthenticateAsync(
                    emailSettings["SmtpUsername"], 
                    emailSettings["SmtpPassword"]
                );

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendEmailConfirmationAsync(string toEmail, string confirmationLink)
        {
            var subject = "Confirm your email address";
            var body = $@"
                <html>
                <body>
                    <h2>Welcome to OPROZ!</h2>
                    <p>Please confirm your email address by clicking the link below:</p>
                    <p><a href='{confirmationLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Confirm Email</a></p>
                    <p>If you cannot click the button, copy and paste this link into your browser:</p>
                    <p>{confirmationLink}</p>
                    <p>Best regards,<br>OPROZ Team</p>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetAsync(string toEmail, string resetLink)
        {
            var subject = "Reset your password";
            var body = $@"
                <html>
                <body>
                    <h2>Password Reset Request</h2>
                    <p>We received a request to reset your password. Click the link below to reset it:</p>
                    <p><a href='{resetLink}' style='background-color: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                    <p>If you cannot click the button, copy and paste this link into your browser:</p>
                    <p>{resetLink}</p>
                    <p>If you didn't request this reset, please ignore this email.</p>
                    <p>Best regards,<br>OPROZ Team</p>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendContactFormEmailAsync(string name, string email, string phone, string subject, string message)
        {
            var emailSubject = $"Contact Form Submission: {subject}";
            var emailBody = $@"
                <html>
                <body>
                    <h2>New Contact Form Submission</h2>
                    <p><strong>Name:</strong> {name}</p>
                    <p><strong>Email:</strong> {email}</p>
                    <p><strong>Phone:</strong> {phone ?? "Not provided"}</p>
                    <p><strong>Subject:</strong> {subject}</p>
                    <p><strong>Message:</strong></p>
                    <p>{message}</p>
                    <hr>
                    <p>This message was sent through the OPROZ contact form.</p>
                </body>
                </html>";

            var supportEmail = _configuration["ApplicationSettings:SupportEmail"] ?? "support@oproz.com";
            await SendEmailAsync(supportEmail, emailSubject, emailBody);
        }
    }
}