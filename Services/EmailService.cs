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

        public async Task SendPaymentSuccessEmailAsync(string toEmail, string userName, string planName, decimal amount, string transactionId, DateTime subscriptionEndDate)
        {
            var subject = "Payment Successful - OPROZ Subscription Confirmed";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #28a745; color: white; padding: 20px; text-align: center;'>
                        <h1>Payment Successful!</h1>
                    </div>
                    <div style='padding: 20px;'>
                        <h2>Dear {userName},</h2>
                        <p>Thank you for your payment! Your subscription has been successfully activated.</p>
                        
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <h3>Payment Details:</h3>
                            <p><strong>Plan:</strong> {planName}</p>
                            <p><strong>Amount Paid:</strong> ₹{amount:F2}</p>
                            <p><strong>Transaction ID:</strong> {transactionId}</p>
                            <p><strong>Subscription Valid Until:</strong> {subscriptionEndDate:yyyy-MM-dd}</p>
                        </div>
                        
                        <p>You can now access all the features included in your {planName} plan.</p>
                        <p>If you have any questions or need assistance, please don't hesitate to contact our support team.</p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{_configuration["ApplicationSettings:ApplicationUrl"]}/Payment/History' 
                               style='background-color: #007bff; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                               View Payment History
                            </a>
                        </div>
                        
                        <p>Best regards,<br>OPROZ Team</p>
                    </div>
                    <div style='background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #666;'>
                        <p>This is an automated message. Please do not reply to this email.</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPaymentFailureEmailAsync(string toEmail, string userName, string planName, decimal amount, string transactionId, string reason = "")
        {
            var subject = "Payment Failed - OPROZ Subscription";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #dc3545; color: white; padding: 20px; text-align: center;'>
                        <h1>Payment Failed</h1>
                    </div>
                    <div style='padding: 20px;'>
                        <h2>Dear {userName},</h2>
                        <p>Unfortunately, your payment could not be processed successfully.</p>
                        
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <h3>Payment Details:</h3>
                            <p><strong>Plan:</strong> {planName}</p>
                            <p><strong>Amount:</strong> ₹{amount:F2}</p>
                            <p><strong>Transaction ID:</strong> {transactionId}</p>
                            {(string.IsNullOrEmpty(reason) ? "" : $"<p><strong>Reason:</strong> {reason}</p>")}
                        </div>
                        
                        <h3>What you can do:</h3>
                        <ul>
                            <li>Check if your card has sufficient funds</li>
                            <li>Verify your card details are correct</li>
                            <li>Try using a different payment method</li>
                            <li>Contact your bank if the issue persists</li>
                        </ul>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{_configuration["ApplicationSettings:ApplicationUrl"]}/Services' 
                               style='background-color: #007bff; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                               Try Again
                            </a>
                        </div>
                        
                        <p>If you continue to experience issues, please contact our support team at {_configuration["ApplicationSettings:SupportEmail"]}.</p>
                        
                        <p>Best regards,<br>OPROZ Team</p>
                    </div>
                    <div style='background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #666;'>
                        <p>This is an automated message. Please do not reply to this email.</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPaymentPendingEmailAsync(string toEmail, string userName, string planName, decimal amount, string transactionId)
        {
            var subject = "Payment Pending - OPROZ Subscription";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #ffc107; color: #212529; padding: 20px; text-align: center;'>
                        <h1>Payment Pending</h1>
                    </div>
                    <div style='padding: 20px;'>
                        <h2>Dear {userName},</h2>
                        <p>Your payment is currently being processed. We'll notify you once it's confirmed.</p>
                        
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <h3>Payment Details:</h3>
                            <p><strong>Plan:</strong> {planName}</p>
                            <p><strong>Amount:</strong> ₹{amount:F2}</p>
                            <p><strong>Transaction ID:</strong> {transactionId}</p>
                            <p><strong>Status:</strong> Pending Confirmation</p>
                        </div>
                        
                        <p>Processing time typically takes a few minutes to a few hours, depending on your payment method.</p>
                        <p>You'll receive another email once your payment is confirmed and your subscription is activated.</p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{_configuration["ApplicationSettings:ApplicationUrl"]}/Payment/History' 
                               style='background-color: #007bff; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                               Check Payment Status
                            </a>
                        </div>
                        
                        <p>If you have any questions, please contact our support team at {_configuration["ApplicationSettings:SupportEmail"]}.</p>
                        
                        <p>Best regards,<br>OPROZ Team</p>
                    </div>
                    <div style='background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #666;'>
                        <p>This is an automated message. Please do not reply to this email.</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}