using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.CommandHandler
{
    /// <summary>
    /// Handler for updating users.
    /// The validator ensures the user exists before this handler is called.
    /// If validation fails (user not found), this handler will not be executed.
    /// </summary>
    public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public UpdateUserCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Updates the user in the database.
        /// Assumes the user exists (validated by UpdateUserCommandValidator).
        /// </summary>
        /// <param name="command">The update command containing the user ID and updated data</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Successful command response with the updated user</returns>
        public async Task<ICommandResponse> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new CommandResponse<MindedExample.Domain.User>(default(MindedExample.Domain.User), false);
            }

            MindedExample.Domain.User user = await _context.Users
                .SingleOrDefaultAsync(u => u.Id == command.UserId && u.TenantId == _currentUserAccessor.TenantId.Value, cancellationToken);

            // Update user properties
            // Note: user should never be null here due to validation, but defensive programming
            if (user != null)
            {
                user.Name = command.User.Name;
                user.Surname = command.User.Surname;
                user.Email = command.User.Email;

                await _context.SaveChangesAsync(cancellationToken);
            }

            return new CommandResponse<MindedExample.Domain.User>(command.User)
            {
                Successful = true
            };
        }
    }
}

