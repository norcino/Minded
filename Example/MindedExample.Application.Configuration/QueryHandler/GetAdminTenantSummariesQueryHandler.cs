using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Configuration.Query;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Application.Configuration.QueryHandler
{
    /// <summary>
    /// Handles retrieval of tenant summaries for global administration.
    /// </summary>
    public class GetAdminTenantSummariesQueryHandler : IQueryHandler<GetAdminTenantSummariesQuery, IQueryResponse<IEnumerable<TenantSummaryModel>>>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetAdminTenantSummariesQueryHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<IQueryResponse<IEnumerable<TenantSummaryModel>>> HandleAsync(GetAdminTenantSummariesQuery query, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.IsGlobalAdmin)
            {
                throw new SecurityException();
            }

            var tenants = await _context.Tenants
                .AsNoTracking()
                .Include(t => t.LegalOwnerUser)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

            var tenantIds = tenants.Select(t => t.Id).ToArray();

            var activeUsersByTenant = await _context.Users
                .AsNoTracking()
                .Where(u => u.TenantId.HasValue && tenantIds.Contains(u.TenantId.Value) && u.IsActive)
                .GroupBy(u => u.TenantId.Value)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

            var categoriesByTenant = await _context.Categories
                .AsNoTracking()
                .Where(c => c.User.TenantId.HasValue && tenantIds.Contains(c.User.TenantId.Value))
                .GroupBy(c => c.User.TenantId.Value)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

            var transactionsByTenant = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.User.TenantId.HasValue && tenantIds.Contains(t.User.TenantId.Value))
                .GroupBy(t => t.User.TenantId.Value)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

            var result = tenants.Select(t => new TenantSummaryModel
            {
                Id = t.Id,
                Name = t.Name,
                LegalOwnerUserId = t.LegalOwnerUserId,
                LegalOwnerEmail = t.LegalOwnerUser?.Email,
                ActiveUsersCount = activeUsersByTenant.TryGetValue(t.Id, out var usersCount) ? usersCount : 0,
                CategoriesCount = categoriesByTenant.TryGetValue(t.Id, out var categoriesCount) ? categoriesCount : 0,
                TransactionsCount = transactionsByTenant.TryGetValue(t.Id, out var transactionsCount) ? transactionsCount : 0
            }).ToList();

            return new QueryResponse<IEnumerable<TenantSummaryModel>>(result);
        }
    }

}

