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

        public AssignRolesToUserCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<ICommandResponse> HandleAsync(AssignRolesToUserCommand command, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

            if (user == null)
            {
                return new CommandResponse { Successful = false };
            }

            if (_context is MindedExampleContext concreteContext)
            {
                // Clear existing roles for this user
                await concreteContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM UserRoles WHERE UserId = {0}", command.UserId);

                // Insert new roles
                foreach (var roleName in command.RoleNames)
                {
                    await concreteContext.Database.ExecuteSqlRawAsync(
                        "INSERT INTO UserRoles (UserId, RoleName) VALUES ({0}, {1})",
                        command.UserId, roleName);
                }
            }

            return new CommandResponse { Successful = true };
        }
    }
}
