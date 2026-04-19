using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Role.Command;

namespace MindedExample.Application.Role.CommandHandler
{
    public class UpdateRolePermissionsCommandHandler : ICommandHandler<UpdateRolePermissionsCommand>
    {
        private readonly IMindedExampleContext _context;

        public UpdateRolePermissionsCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<ICommandResponse> HandleAsync(UpdateRolePermissionsCommand command, CancellationToken cancellationToken = default)
        {
            if (_context is MindedExampleContext concreteContext)
            {
                // Clear existing permissions for this role
                await concreteContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM RolePermissions WHERE RoleName = {0}", command.RoleName);

                // Insert new permissions
                foreach (var permissionName in command.PermissionNames)
                {
                    await concreteContext.Database.ExecuteSqlRawAsync(
                        "INSERT INTO RolePermissions (RoleName, PermissionName) VALUES ({0}, {1})",
                        command.RoleName, permissionName);
                }
            }

            return new CommandResponse { Successful = true };
        }
    }
}
