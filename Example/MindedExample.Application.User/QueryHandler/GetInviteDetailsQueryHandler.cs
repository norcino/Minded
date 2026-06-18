using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.User.Query;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Application.User.QueryHandler
{
    /// <summary>
    /// Handles <see cref="GetInviteDetailsQuery"/> by looking up an active tenant invite
    /// identified by its token or short code. Returns <c>null</c> when the invite does not
    /// exist, has already been used, or has expired — causing the REST mediator to respond
    /// with 404 Not Found.
    /// </summary>
    public class GetInviteDetailsQueryHandler : IQueryHandler<GetInviteDetailsQuery, InviteDetailsResult>
    {
        private readonly IMindedExampleContext _context;

        /// <summary>
        /// Initializes a new <see cref="GetInviteDetailsQueryHandler"/>.
        /// </summary>
        public GetInviteDetailsQueryHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<InviteDetailsResult> HandleAsync(GetInviteDetailsQuery query, CancellationToken cancellationToken = default)
        {
            var invite = await _context.TenantInvites
                .Include(i => i.Tenant)
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    i => i.Token == query.TokenOrCode || i.Code == query.TokenOrCode,
                    cancellationToken);

            if (invite == null || invite.UsedAtUtc != null || invite.ExpiresAtUtc < DateTime.UtcNow)
                return null;

            return new InviteDetailsResult
            {
                TenantName = invite.Tenant?.Name,
                Email = invite.Email
            };
        }
    }
}
