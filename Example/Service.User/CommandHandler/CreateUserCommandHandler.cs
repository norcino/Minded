using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Minded.Framework.CQRS.Command;
using Service.User.Command;

namespace Service.User.CommandHandler
{
    /// <summary>
    /// Handler for creating new users.
    /// The validator ensures the user data is valid before this handler is called.
    /// If validation fails, this handler will not be executed.
    /// </summary>
    public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Data.Entity.User>
    {
        private readonly IMindedExampleContext _context;

        public CreateUserCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a new user in the database.
        /// Assumes the user data is valid (validated by CreateUserCommandValidator).
        /// </summary>
        /// <param name="command">The create command containing the user data</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Successful command response with the created user</returns>
        public async Task<ICommandResponse<Data.Entity.User>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
        {
            _context.Users.Add(command.User);
            await _context.SaveChangesAsync(cancellationToken);

            return new CommandResponse<Data.Entity.User>(command.User)
            {
                Successful = true
            };
        }
    }
}

