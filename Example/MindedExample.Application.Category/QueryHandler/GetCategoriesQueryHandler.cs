using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Category.Query;

namespace MindedExample.Application.Category.QueryHandler
{
    public class GetCategoriesQueryHandler : IQueryHandler<GetCategoriesQuery, IQueryResponse<IEnumerable<MindedExample.Domain.Category>>>
    {
        private readonly IMindedExampleContext _context;
        private readonly ILogger<IQueryHandler<GetCategoriesQuery, IQueryResponse<IEnumerable<MindedExample.Domain.Category>>>> _logger;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetCategoriesQueryHandler(
            IMindedExampleContext context,
            ILogger<IQueryHandler<GetCategoriesQuery, IQueryResponse<IEnumerable<MindedExample.Domain.Category>>>> logger,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _logger = logger;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<IQueryResponse<IEnumerable<MindedExample.Domain.Category>>> HandleAsync(GetCategoriesQuery query, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new QueryResponse<IEnumerable<MindedExample.Domain.Category>>(new List<MindedExample.Domain.Category>());
            }

            var tenantId = _currentUserAccessor.TenantId.Value;
            var categoriesQuery = _context.Categories.Where(c => c.User.TenantId == tenantId).AsQueryable();
            List<MindedExample.Domain.Category> result = await query.ApplyQueryTo(categoriesQuery).ToListAsync(cancellationToken);
            var response = new QueryResponse<IEnumerable<MindedExample.Domain.Category>>(result);

            return response;
        }
    } 
}
