using System;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.CommandHandler
{
    /// <summary>
    /// Handler for rejecting a pending tenant join request.
    /// Marks the request as processed and not approved.
    /// </summary>
    public class RejectTenantJoinRequestCommandHandler : ICommandHandler<RejectTenantJoinRequestCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        /// <summary>
        /// Initializes a new <see cref="RejectTenantJoinRequestCommandHandler"/>.
        /// </summary>
        public RejectTenantJoinRequestCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <inheritdoc />
        public async Task<ICommandResponse> HandleAsync(RejectTenantJoinRequestCommand command, CancellationToken cancellationToken = default)
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

            request.ProcessedAtUtc = DateTime.UtcNow;
            request.ProcessedByUserId = _currentUserAccessor.UserId.Value;
            request.Approved = false;
            await _context.SaveChangesAsync(cancellationToken);

            return CommandResponse.Success();
        }
    }
}
