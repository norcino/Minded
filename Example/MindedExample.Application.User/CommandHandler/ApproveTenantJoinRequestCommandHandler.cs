using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.User.Command;
using MindedExample.Domain;

namespace MindedExample.Application.User.CommandHandler
{
    /// <summary>
    /// Handler for approving a pending tenant join request.
    /// Creates a user account from the request data and assigns the default User role.
    /// If a user with the same email already exists, the request is rejected instead.
    /// </summary>
    public class ApproveTenantJoinRequestCommandHandler : ICommandHandler<ApproveTenantJoinRequestCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        /// <summary>
        /// Initializes a new <see cref="ApproveTenantJoinRequestCommandHandler"/>.
        /// </summary>
        public ApproveTenantJoinRequestCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <inheritdoc />
        public async Task<ICommandResponse> HandleAsync(ApproveTenantJoinRequestCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue || !_currentUserAccessor.UserId.HasValue)
            {
                return new CommandResponse { Successful = false };
            }

            var request = await _context.TenantJoinRequests
                .SingleOrDefaultAsync(
                    r => r.Id == command.RequestId && r.TenantId == _currentUserAccessor.TenantId.Value && r.ProcessedAtUtc == null,
                    cancellationToken);

            if (request == null)
            {
                return new CommandResponse { Successful = false };
            }

            // If the email is already taken, reject the request instead of approving.
            var emailConflict = await _context.Users.AsNoTracking()
                .AnyAsync(u => u.Email == request.Email, cancellationToken);

            if (emailConflict)
            {
                request.ProcessedAtUtc = DateTime.UtcNow;
                request.ProcessedByUserId = _currentUserAccessor.UserId.Value;
                request.Approved = false;
                await _context.SaveChangesAsync(cancellationToken);
                return new CommandResponse { Successful = false };
            }

            var newUser = new MindedExample.Domain.User
            {
                Name = request.Name,
                Surname = request.Surname,
                Email = request.Email,
                PasswordHash = request.PasswordHash,
                TenantId = request.TenantId,
                TenantRole = TenantMemberRoles.Member,
                IsActive = true,
                IsGlobalAdmin = false
            };

            _context.Users.Add(newUser);

            request.ProcessedAtUtc = DateTime.UtcNow;
            request.ProcessedByUserId = _currentUserAccessor.UserId.Value;
            request.Approved = true;

            await _context.SaveChangesAsync(cancellationToken);

            if (_context is MindedExampleContext concreteContext)
            {
                concreteContext.Set<Dictionary<string, object>>("UserRoles").Add(new Dictionary<string, object>
                {
                    ["TenantId"] = request.TenantId,
                    ["UserId"] = newUser.Id,
                    ["RoleName"] = Roles.User
                });
                await concreteContext.SaveChangesAsync(cancellationToken);
            }

            return CommandResponse.Success();
        }
    }
}
