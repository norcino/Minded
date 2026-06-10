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
                // Remove all permission assignments for this role
                await concreteContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM RolePermissions WHERE TenantId = {0} AND RoleName = {1}", tenantId, command.RoleName);

                // Remove all user assignments for this role
                await concreteContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM UserRoles WHERE TenantId = {0} AND RoleName = {1}", tenantId, command.RoleName);
            }

            return new CommandResponse { Successful = true };
        }
    }
}
