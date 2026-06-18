using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using MindedExample.Domain;
using Minded.Framework.CQRS.Command;
using Minded.Framework.Mediator;
using MindedExample.Application.User.Command;
using MindedExample.Application.Role.Command;

namespace MindedExample.Application.User.CommandHandler
{
    /// <summary>
    /// Handler for creating new users within the current tenant.
    /// The validator ensures the user data is valid before this handler is called.
    /// After the user is persisted, the default role is assigned via <see cref="AssignRolesToUserCommand"/>
    /// so that orchestration stays in the handler rather than in the controller.
    /// </summary>
    public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, MindedExample.Domain.User>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new <see cref="CreateUserCommandHandler"/>.
        /// </summary>
        public CreateUserCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor, IMediator mediator)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _mediator = mediator;
        }

        /// <summary>
        /// Creates a new user in the database and assigns the default role.
        /// Assumes the user data is valid (validated by CreateUserCommandValidator).
        /// </summary>
        /// <param name="command">The create command containing the user data.</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
        /// <returns>Successful command response with the created user.</returns>
        public async Task<ICommandResponse<MindedExample.Domain.User>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new CommandResponse<MindedExample.Domain.User>(default(MindedExample.Domain.User), false);
            }

            command.User.TenantId = _currentUserAccessor.TenantId.Value;

            // Tenant role is managed exclusively through the tenant-admin endpoints;
            // users created here always start as members regardless of the payload value.
            command.User.TenantRole = TenantMemberRoles.Member;
            _context.Users.Add(command.User);
            await _context.SaveChangesAsync(cancellationToken);

            // Assign the default role to the newly created user.
            if (command.User.Id > 0)
            {
                await _mediator.ProcessCommandAsync(
                    new AssignRolesToUserCommand(command.User.Id, new List<string> { DefaultRolesDefinition.DefaultRole }, command.TraceId),
                    cancellationToken);
            }

            return new CommandResponse<MindedExample.Domain.User>(command.User)
            {
                Successful = true
            };
        }
    }
}

