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
                // Mutations go through the shared-type entity set (not raw SQL) so EF
                // generates correctly quoted, schema-qualified SQL for every provider.
                var userRoles = concreteContext.Set<Dictionary<string, object>>("UserRoles");

                // Clear existing roles for this user
                var existingRoles = await userRoles
                    .Where(ur => (int)ur["TenantId"] == tenantId && (int)ur["UserId"] == command.UserId)
                    .ToListAsync(cancellationToken);
                userRoles.RemoveRange(existingRoles);
                await concreteContext.SaveChangesAsync(cancellationToken);

                // Insert new roles
                foreach (var roleName in command.RoleNames)
                {
                    userRoles.Add(new Dictionary<string, object>
                    {
                        ["TenantId"] = tenantId,
                        ["UserId"] = command.UserId,
                        ["RoleName"] = roleName
                    });
                }

                await concreteContext.SaveChangesAsync(cancellationToken);
            }

            return new CommandResponse { Successful = true };
        }
    }
}
