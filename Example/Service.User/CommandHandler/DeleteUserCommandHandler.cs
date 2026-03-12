using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using Service.User.Command;

namespace Service.User.CommandHandler
{
    /// <summary>
    /// Handler for deleting users.
    /// The validator ensures the user exists before this handler is called.
    /// If validation fails (user not found), this handler will not be executed.
    /// </summary>
    public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
    {
        private readonly IMindedExampleContext _context;

        public DeleteUserCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Deletes the user from the database.
        /// Assumes the user exists (validated by DeleteUserCommandValidator).
        /// </summary>
        /// <param name="command">The delete command containing the user ID</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Successful command response</returns>
        public async Task<ICommandResponse> HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
        {
            Data.Entity.User user = await _context.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

            // Note: user should never be null here due to validation, but defensive programming
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return CommandResponse.Success();
        }
    }
}

