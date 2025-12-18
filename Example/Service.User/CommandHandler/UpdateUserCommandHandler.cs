using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using Service.User.Command;

namespace Service.User.CommandHandler
{
    /// <summary>
    /// Handler for updating users.
    /// The validator ensures the user exists before this handler is called.
    /// If validation fails (user not found), this handler will not be executed.
    /// </summary>
    public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
    {
        private readonly IMindedExampleContext _context;

        public UpdateUserCommandHandler(IMindedExampleContext context)
        {
            _context = context;
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
            Data.Entity.User user = await _context.Users.SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

            // Update user properties
            // Note: user should never be null here due to validation, but defensive programming
            if (user != null)
            {
                user.Name = command.User.Name;
                user.Surname = command.User.Surname;
                user.Email = command.User.Email;

                await _context.SaveChangesAsync(cancellationToken);
            }

            return new CommandResponse<Data.Entity.User>(command.User)
            {
                Successful = true
            };
        }
    }
}

