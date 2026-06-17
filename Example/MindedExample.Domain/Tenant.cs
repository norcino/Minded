using System.Collections.Generic;

namespace MindedExample.Domain
{
    /// <summary>
    /// Represents a tenant in the multi-tenant system.
    /// A tenant groups users and all business data in the same database.
    /// </summary>
    public class Tenant
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int? LegalOwnerUserId { get; set; }

        public virtual User LegalOwnerUser { get; set; }

        public virtual ICollection<User> Users { get; set; } = new HashSet<User>();

        public virtual ICollection<TenantInvite> Invites { get; set; } = new HashSet<TenantInvite>();

        public virtual ICollection<TenantJoinRequest> JoinRequests { get; set; } = new HashSet<TenantJoinRequest>();
    }
}
