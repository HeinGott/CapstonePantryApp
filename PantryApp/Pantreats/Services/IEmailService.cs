namespace Pantreats.Services
{
    public interface IEmailService
    {
        Task<bool> SendOrderConfirmationAsync(string toEmail, int orderId, string orderUrl);
    }
}
