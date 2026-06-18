using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.CommandHandler
{
    /// <summary>
    /// Handler for updating a tenant user's role.
    /// Assumes the user exists in the current tenant and the role is valid (validated by <see cref="Validator.UpdateTenantUserRoleCommandValidator"/>).
    /// </summary>
    public class UpdateTenantUserRoleCommandHandler : ICommandHandler<UpdateTenantUserRoleCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        /// <summary>
        /// Initializes a new <see cref="UpdateTenantUserRoleCommandHandler"/>.
        /// </summary>
        public UpdateTenantUserRoleCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <inheritdoc />
        public async Task<ICommandResponse> HandleAsync(UpdateTenantUserRoleCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new CommandResponse { Successful = false };
            }

            var user = await _context.Users
                .SingleOrDefaultAsync(
                    u => u.Id == command.UserId && u.TenantId == _currentUserAccessor.TenantId.Value,
                    cancellationToken);

            if (user == null)
            {
                return new CommandResponse { Successful = false };
            }

            user.TenantRole = command.Role.Trim();
            await _context.SaveChangesAsync(cancellationToken);

            return CommandResponse.Success();
        }
    }
}
