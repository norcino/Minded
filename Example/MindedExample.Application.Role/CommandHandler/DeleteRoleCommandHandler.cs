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

        public DeleteRoleCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<ICommandResponse> HandleAsync(DeleteRoleCommand command, CancellationToken cancellationToken = default)
        {
            if (_context is MindedExampleContext concreteContext)
            {
                // Remove all permission assignments for this role
                await concreteContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM RolePermissions WHERE RoleName = {0}", command.RoleName);

                // Remove all user assignments for this role
                await concreteContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM UserRoles WHERE RoleName = {0}", command.RoleName);
            }

            return new CommandResponse { Successful = true };
        }
    }
}
