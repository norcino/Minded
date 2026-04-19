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

        public ResetRolesToDefaultCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<ICommandResponse> HandleAsync(ResetRolesToDefaultCommand command, CancellationToken cancellationToken = default)
        {
            if (_context is MindedExampleContext concreteContext)
            {
                // Clear all existing role-permission mappings
                await concreteContext.Database.ExecuteSqlRawAsync("DELETE FROM RolePermissions", cancellationToken);

                // Re-insert defaults from DefaultRolesDefinition
                foreach (var kvp in DefaultRolesDefinition.RolePermissions)
                {
                    foreach (var permission in kvp.Value)
                    {
                        await concreteContext.Database.ExecuteSqlRawAsync(
                            "INSERT INTO RolePermissions (RoleName, PermissionName) VALUES ({0}, {1})",
                            kvp.Key, permission);
                    }
                }
            }

            return new CommandResponse { Successful = true };
        }
    }
}
