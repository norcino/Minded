using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.User.Query;

namespace MindedExample.Application.User.QueryHandler
{
    /// <summary>
    /// Handler for retrieving pending tenant join requests for the tenant administration view.
    /// Returns a lightweight DTO ordered by creation time, excluding sensitive data such as password hash.
    /// </summary>
    public class GetTenantJoinRequestsQueryHandler : IQueryHandler<GetTenantJoinRequestsQuery, IQueryResponse<IEnumerable<TenantJoinRequestSummaryDto>>>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        /// <summary>
        /// Initializes a new <see cref="GetTenantJoinRequestsQueryHandler"/>.
        /// </summary>
        public GetTenantJoinRequestsQueryHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <inheritdoc />
        public async Task<IQueryResponse<IEnumerable<TenantJoinRequestSummaryDto>>> HandleAsync(
            GetTenantJoinRequestsQuery query, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new QueryResponse<IEnumerable<TenantJoinRequestSummaryDto>>(new List<TenantJoinRequestSummaryDto>());
            }

            var requests = await _context.TenantJoinRequests
                .AsNoTracking()
                .Where(r => r.TenantId == _currentUserAccessor.TenantId.Value && r.ProcessedAtUtc == null)
                .OrderBy(r => r.CreatedAtUtc)
                .Select(r => new TenantJoinRequestSummaryDto
                {
                    Id = r.Id,
                    TenantId = r.TenantId,
                    Name = r.Name,
                    Surname = r.Surname,
                    Email = r.Email,
                    CreatedAtUtc = r.CreatedAtUtc.ToString("O")
                })
                .ToListAsync(cancellationToken);

            return new QueryResponse<IEnumerable<TenantJoinRequestSummaryDto>>(requests);
        }
    }
}
