using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Role.Command;

namespace MindedExample.Application.Role.CommandHandler
{
    public class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand>
    {
        private readonly IMindedExampleContext _context;

        public CreateRoleCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<ICommandResponse> HandleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
        {
            // Role is just a name now - creating a role means it can appear in RolePermissions
            // We insert a placeholder row so the role "exists" (with no permissions initially)
            // Actually, roles exist implicitly. But we can validate by checking RolePermissions.
            // For now, just return success - the role name is valid once used in assignments.
            return await Task.FromResult<ICommandResponse>(new CommandResponse { Successful = true });
        }
    }
}
