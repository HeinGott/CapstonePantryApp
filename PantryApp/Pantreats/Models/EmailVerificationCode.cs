using System.ComponentModel.DataAnnotations;
namespace Pantreats.Models
{
    public class EmailVerificationCode
    {
        public int EmailVerificationCodeId { get; set; }
        public string UserId { get; set; }

        public string Code { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        public int FailedAttempts { get; set; } = 0;
    }
}
