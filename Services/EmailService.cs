using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using OPROZ_Main.Models;

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

        public async Task SendPaymentSuccessEmailAsync(string toEmail, string userName, PaymentHistory paymentHistory)
        {
            var subject = "Payment Successful - Subscription Activated";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #28a745;'>Payment Successful!</h2>
                        <p>Dear {userName},</p>
                        <p>Thank you for your payment. Your subscription has been successfully activated.</p>
                        
                        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                            <h3 style='margin-top: 0; color: #007bff;'>Payment Details</h3>
                            <p><strong>Transaction ID:</strong> {paymentHistory.TransactionId}</p>
                            <p><strong>Plan:</strong> {paymentHistory.SubscriptionPlan?.Name ?? "N/A"}</p>
                            <p><strong>Amount Paid:</strong> ₹{paymentHistory.FinalAmount:F2}</p>
                            <p><strong>Payment Date:</strong> {paymentHistory.PaymentDate:dd MMM yyyy, HH:mm}</p>
                            <p><strong>Subscription Period:</strong> {paymentHistory.SubscriptionStartDate:dd MMM yyyy} - {paymentHistory.SubscriptionEndDate:dd MMM yyyy}</p>
                        </div>
                        
                        <p>You can now access all features included in your subscription plan.</p>
                        <p>If you have any questions, feel free to contact our support team.</p>
                        
                        <p>Best regards,<br>OPROZ Team</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPaymentFailureEmailAsync(string toEmail, string userName, string planName, decimal amount)
        {
            var subject = "Payment Failed - Please Try Again";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #dc3545;'>Payment Failed</h2>
                        <p>Dear {userName},</p>
                        <p>We were unable to process your payment for the {planName} subscription plan.</p>
                        
                        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                            <h3 style='margin-top: 0; color: #dc3545;'>Payment Details</h3>
                            <p><strong>Plan:</strong> {planName}</p>
                            <p><strong>Amount:</strong> ₹{amount:F2}</p>
                            <p><strong>Status:</strong> Failed</p>
                        </div>
                        
                        <p>Please check your payment method and try again. Common reasons for payment failure include:</p>
                        <ul>
                            <li>Insufficient balance</li>
                            <li>Incorrect card details</li>
                            <li>Card expired</li>
                            <li>Bank security restrictions</li>
                        </ul>
                        
                        <p><a href='#' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 10px 0;'>Try Again</a></p>
                        
                        <p>If you continue to experience issues, please contact our support team.</p>
                        
                        <p>Best regards,<br>OPROZ Team</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPaymentPendingEmailAsync(string toEmail, string userName, string planName, decimal amount)
        {
            var subject = "Payment Pending - We're Processing Your Transaction";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #ffc107;'>Payment Processing</h2>
                        <p>Dear {userName},</p>
                        <p>Your payment for the {planName} subscription is currently being processed.</p>
                        
                        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                            <h3 style='margin-top: 0; color: #ffc107;'>Payment Details</h3>
                            <p><strong>Plan:</strong> {planName}</p>
                            <p><strong>Amount:</strong> ₹{amount:F2}</p>
                            <p><strong>Status:</strong> Processing</p>
                        </div>
                        
                        <p>This process usually takes a few minutes. You will receive another email once your payment is confirmed and your subscription is activated.</p>
                        
                        <p>If you don't receive confirmation within 24 hours, please contact our support team with your transaction details.</p>
                        
                        <p>Best regards,<br>OPROZ Team</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}