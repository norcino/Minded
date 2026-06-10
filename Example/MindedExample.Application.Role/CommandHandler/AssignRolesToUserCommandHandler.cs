using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Role.Command;

namespace MindedExample.Application.Role.CommandHandler
{
    public class AssignRolesToUserCommandHandler : ICommandHandler<AssignRolesToUserCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public AssignRolesToUserCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ICommandResponse> HandleAsync(AssignRolesToUserCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new CommandResponse { Successful = false };
            }

            var tenantId = _currentUserAccessor.TenantId.Value;
            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Id == command.UserId && u.TenantId == tenantId, cancellationToken);

            if (user == null)
            {
                return new CommandResponse { Successful = false };
            }

            if (_context is MindedExampleContext concreteContext)
            {
                // Clear existing roles for this user
                await concreteContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM UserRoles WHERE TenantId = {0} AND UserId = {1}", tenantId, command.UserId);

                // Insert new roles
                foreach (var roleName in command.RoleNames)
                {
                    await concreteContext.Database.ExecuteSqlRawAsync(
                        "INSERT INTO UserRoles (TenantId, UserId, RoleName) VALUES ({0}, {1}, {2})",
                        tenantId, command.UserId, roleName);
                }
            }

            return new CommandResponse { Successful = true };
        }
    }
}
