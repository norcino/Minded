using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Configuration.Command;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Application.Configuration.CommandHandler
{
    /// <summary>
    /// Handles tenant deletion with full tenant-scoped data cleanup.
    /// Authorization (global admin required) is enforced by the [RequireClaim] attribute on the command.
    /// </summary>
    public class DeleteTenantCommandHandler : ICommandHandler<DeleteTenantCommand>
    {
        private readonly IMindedExampleContext _context;

        public DeleteTenantCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<ICommandResponse> HandleAsync(DeleteTenantCommand command, CancellationToken cancellationToken = default)
        {
            var tenant = await _context.Tenants
                .AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == command.TenantId, cancellationToken);

            if (tenant == null)
            {
                return new CommandResponse
                {
                    Successful = false
                };
            }

            var tenantUserIds = await _context.Users
                .AsNoTracking()
                .Where(u => u.TenantId == command.TenantId)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            if (_context is MindedExampleContext concreteContext)
            {
                // Deletes go through the shared-type entity sets (not raw SQL) so EF
                // generates correctly quoted, schema-qualified SQL for every provider.
                // Loaded and removed via the change tracker: ExecuteDelete cannot translate
                // the shared-type dictionary indexer access.
                var userRoles = concreteContext.Set<Dictionary<string, object>>("UserRoles");
                var userRoleRows = await userRoles
                    .Where(ur => (int)ur["TenantId"] == command.TenantId)
                    .ToListAsync(cancellationToken);
                userRoles.RemoveRange(userRoleRows);

                var rolePermissions = concreteContext.Set<Dictionary<string, object>>("RolePermissions");
                var rolePermissionRows = await rolePermissions
                    .Where(rp => (int)rp["TenantId"] == command.TenantId)
                    .ToListAsync(cancellationToken);
                rolePermissions.RemoveRange(rolePermissionRows);

                await concreteContext.SaveChangesAsync(cancellationToken);
            }

            await _context.TenantInvites.Where(i => i.TenantId == command.TenantId).ExecuteDeleteAsync(cancellationToken);
            await _context.TenantJoinRequests.Where(r => r.TenantId == command.TenantId).ExecuteDeleteAsync(cancellationToken);
            await _context.Transactions.Where(t => t.User.TenantId == command.TenantId).ExecuteDeleteAsync(cancellationToken);
            await _context.Categories.Where(c => c.User.TenantId == command.TenantId).ExecuteDeleteAsync(cancellationToken);

            if (tenantUserIds.Count > 0)
            {
                await _context.PasswordResetTokens.Where(t => tenantUserIds.Contains(t.UserId)).ExecuteDeleteAsync(cancellationToken);

                // The tenant references its legal owner with a Restrict foreign key;
                // detach it before the users can be deleted.
                await _context.Tenants.Where(t => t.Id == command.TenantId)
                    .ExecuteUpdateAsync(t => t.SetProperty(x => x.LegalOwnerUserId, (int?)null), cancellationToken);

                await _context.Users.Where(u => tenantUserIds.Contains(u.Id)).ExecuteDeleteAsync(cancellationToken);
            }

            await _context.Tenants.Where(t => t.Id == command.TenantId).ExecuteDeleteAsync(cancellationToken);

            return CommandResponse.Success();
        }
    }

}

