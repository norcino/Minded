using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.CommandHandler
{
    /// <summary>
    /// Handler for removing a user from the current tenant.
    /// Assumes the user exists, is not the legal owner, and that at least one admin will remain (validated by
    /// <see cref="Validator.RemoveTenantUserCommandValidator"/>).
    /// </summary>
    public class RemoveTenantUserCommandHandler : ICommandHandler<RemoveTenantUserCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        /// <summary>
        /// Initializes a new <see cref="RemoveTenantUserCommandHandler"/>.
        /// </summary>
        public RemoveTenantUserCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <inheritdoc />
        public async Task<ICommandResponse> HandleAsync(RemoveTenantUserCommand command, CancellationToken cancellationToken = default)
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

            // Clear FK references on invites accepted by this user so the delete is not
            // blocked by the FK_TenantInvites_UsedByUser Restrict constraint.
            var usedInvites = await _context.TenantInvites
                .Where(i => i.UsedByUserId == user.Id)
                .ToListAsync(cancellationToken);
            foreach (var invite in usedInvites)
                invite.UsedByUserId = null;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);

            return CommandResponse.Success();
        }
    }
}
