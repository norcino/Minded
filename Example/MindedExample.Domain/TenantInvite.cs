using System;
using Minded.Extensions.DataProtection.Abstractions;

namespace MindedExample.Domain
{
    /// <summary>
    /// Represents a tenant invitation that can be redeemed using a code or tokenized link.
    /// </summary>
    public class TenantInvite
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        public int CreatedByUserId { get; set; }

        [SensitiveData]
        public string Email { get; set; }

        public string Code { get; set; }

        public string Token { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UsedAtUtc { get; set; }

        public int? UsedByUserId { get; set; }

        public virtual Tenant Tenant { get; set; }

        public virtual User CreatedByUser { get; set; }

        public virtual User UsedByUser { get; set; }
    }
}
