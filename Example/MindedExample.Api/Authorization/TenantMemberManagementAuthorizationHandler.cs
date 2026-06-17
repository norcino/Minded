using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MindedExample.Domain;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Api.Authorization
{
    /// <summary>
    /// Handles the <see cref="TenantMemberManagementRequirement"/> by verifying that the
    /// current user is an Owner, Admin, or holds the TenantAdmin application role within
    /// their tenant. Global admins are explicitly excluded.
    /// </summary>
    public class TenantMemberManagementAuthorizationHandler
        : AuthorizationHandler<TenantMemberManagementRequirement>
    {
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// Initializes a new <see cref="TenantMemberManagementAuthorizationHandler"/>.
        /// </summary>
        public TenantMemberManagementAuthorizationHandler(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <inheritdoc />
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            TenantMemberManagementRequirement requirement)
        {
            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");
            var tenantIdClaim = context.User.FindFirstValue("tenant_id");

            if (!int.TryParse(userIdClaim, out var userId) || !int.TryParse(tenantIdClaim, out var tenantId))
            {
                return;
            }

            // Global admins are excluded from tenant-level administration.
            var isGlobalAdminClaim = context.User.FindFirstValue("is_global_admin");
            if (bool.TryParse(isGlobalAdminClaim, out var isGlobalAdmin) && isGlobalAdmin)
            {
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IMindedExampleContext>();

            var user = await dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

            if (user == null)
            {
                return;
            }

            if (user.TenantRole == TenantMemberRoles.Owner || user.TenantRole == TenantMemberRoles.Admin)
            {
                context.Succeed(requirement);
                return;
            }

            if (dbContext is MindedExampleContext concreteContext)
            {
                var hasTenantAdminRole = await concreteContext.Set<System.Collections.Generic.Dictionary<string, object>>("UserRoles")
                    .AnyAsync(ur =>
                        (int)ur["TenantId"] == tenantId
                        && (int)ur["UserId"] == userId
                        && (string)ur["RoleName"] == Roles.TenantAdmin);

                if (hasTenantAdminRole)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
