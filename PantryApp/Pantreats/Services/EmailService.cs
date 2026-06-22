using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Pantreats.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendOrderConfirmationAsync(string toEmail, int orderId, string orderUrl)
        {
            try
            {
                var email = _config.GetSection("Email");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Do Not Reply - PanTreats Notification", email["From"]));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = "Your PanTreats order";
                message.Body = new BodyBuilder { HtmlBody = BuildHtml(orderId, orderUrl) }.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(email["Host"], int.Parse(email["Port"]), SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(email["User"], email["Pass"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order confirmation email failed for order {OrderId}", orderId);
                return false;
            }
        }

        private string BuildHtml(int orderId, string orderUrl)
        {
            return $@"
                <p>Your PanTreats order was placed.</p>
                <p><strong>Order :</strong> {orderId}</p>
                <p><a href='{orderUrl}'>View your order</a></p>
                <p>Thanks for ordering with PanTreats! Your order should be ready shortly.</p>";
        }
    }
}
