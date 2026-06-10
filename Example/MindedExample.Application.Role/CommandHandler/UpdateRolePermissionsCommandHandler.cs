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
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public UpdateRolePermissionsCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ICommandResponse> HandleAsync(UpdateRolePermissionsCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new CommandResponse { Successful = false };
            }

            var tenantId = _currentUserAccessor.TenantId.Value;
            if (_context is MindedExampleContext concreteContext)
            {
                // Clear existing permissions for this role
                await concreteContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM RolePermissions WHERE TenantId = {0} AND RoleName = {1}", tenantId, command.RoleName);

                // Insert new permissions
                foreach (var permissionName in command.PermissionNames)
                {
                    await concreteContext.Database.ExecuteSqlRawAsync(
                        "INSERT INTO RolePermissions (TenantId, RoleName, PermissionName) VALUES ({0}, {1}, {2})",
                        tenantId, command.RoleName, permissionName);
                }
            }

            return new CommandResponse { Successful = true };
        }
    }
}
