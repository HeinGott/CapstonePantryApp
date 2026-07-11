namespace Pantreats.Services
{
    public interface IEmailService
    {
        Task<bool> SendOrderConfirmationAsync(string toEmail, int orderId, string orderUrl);
        Task<bool> SendVerificationCodeAsync(string toEmail, string code);
    }
}
