using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Role.Command;

namespace MindedExample.Application.Role.CommandHandler
{
    public class DeleteRoleCommandHandler : ICommandHandler<DeleteRoleCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public DeleteRoleCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ICommandResponse> HandleAsync(DeleteRoleCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new CommandResponse { Successful = false };
            }

            var tenantId = _currentUserAccessor.TenantId.Value;
            if (_context is MindedExampleContext concreteContext)
            {
                // Mutations go through the shared-type entity sets (not raw SQL) so EF
                // generates correctly quoted, schema-qualified SQL for every provider.

                // Remove all permission assignments for this role
                var rolePermissions = concreteContext.Set<Dictionary<string, object>>("RolePermissions");
                var permissionRows = await rolePermissions
                    .Where(rp => (int)rp["TenantId"] == tenantId && (string)rp["RoleName"] == command.RoleName)
                    .ToListAsync(cancellationToken);
                rolePermissions.RemoveRange(permissionRows);

                // Remove all user assignments for this role
                var userRoles = concreteContext.Set<Dictionary<string, object>>("UserRoles");
                var userRoleRows = await userRoles
                    .Where(ur => (int)ur["TenantId"] == tenantId && (string)ur["RoleName"] == command.RoleName)
                    .ToListAsync(cancellationToken);
                userRoles.RemoveRange(userRoleRows);

                await concreteContext.SaveChangesAsync(cancellationToken);
            }

            return new CommandResponse { Successful = true };
        }
    }
}
