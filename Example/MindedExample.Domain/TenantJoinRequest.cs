using System;
using Minded.Extensions.DataProtection.Abstractions;

namespace MindedExample.Domain
{
    /// <summary>
    /// Represents a pending request from a user to join an existing tenant.
    /// </summary>
    public class TenantJoinRequest
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        [SensitiveData]
        public string Name { get; set; }

        [SensitiveData]
        public string Surname { get; set; }

        [SensitiveData]
        public string Email { get; set; }

        [SensitiveData]
        public string PasswordHash { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? ProcessedAtUtc { get; set; }

        public bool Approved { get; set; }

        public int? ProcessedByUserId { get; set; }

        public virtual Tenant Tenant { get; set; }

        public virtual User ProcessedByUser { get; set; }
    }
}
