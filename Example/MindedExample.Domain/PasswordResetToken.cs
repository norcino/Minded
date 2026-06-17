using System;

namespace MindedExample.Domain
{
    /// <summary>
    /// Represents a password reset token for a user account.
    /// </summary>
    public class PasswordResetToken
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Token { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UsedAtUtc { get; set; }

        public virtual User User { get; set; }
    }
}
