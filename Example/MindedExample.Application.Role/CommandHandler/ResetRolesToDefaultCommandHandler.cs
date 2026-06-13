using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Domain;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Role.Command;

namespace MindedExample.Application.Role.CommandHandler
{
    public class ResetRolesToDefaultCommandHandler : ICommandHandler<ResetRolesToDefaultCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ResetRolesToDefaultCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ICommandResponse> HandleAsync(ResetRolesToDefaultCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new CommandResponse { Successful = false };
            }

            var tenantId = _currentUserAccessor.TenantId.Value;
            if (_context is MindedExampleContext concreteContext)
            {
                // Mutations go through the shared-type entity set (not raw SQL) so EF
                // generates correctly quoted, schema-qualified SQL for every provider.
                var rolePermissions = concreteContext.Set<Dictionary<string, object>>("RolePermissions");

                // Clear all existing role-permission mappings
                var existingPermissions = await rolePermissions
                    .Where(rp => (int)rp["TenantId"] == tenantId)
                    .ToListAsync(cancellationToken);
                rolePermissions.RemoveRange(existingPermissions);
                await concreteContext.SaveChangesAsync(cancellationToken);

                // Re-insert defaults from DefaultRolesDefinition
                foreach (var kvp in DefaultRolesDefinition.RolePermissions)
                {
                    foreach (var permission in kvp.Value)
                    {
                        rolePermissions.Add(new Dictionary<string, object>
                        {
                            ["TenantId"] = tenantId,
                            ["RoleName"] = kvp.Key,
                            ["PermissionName"] = permission
                        });
                    }
                }

                await concreteContext.SaveChangesAsync(cancellationToken);
            }

            return new CommandResponse { Successful = true };
        }
    }
}
