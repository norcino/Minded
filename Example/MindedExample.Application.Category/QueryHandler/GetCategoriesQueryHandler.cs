using System.Collections.Generic;
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

        public GetCategoriesQueryHandler(IMindedExampleContext context, ILogger<IQueryHandler<GetCategoriesQuery, IQueryResponse<IEnumerable<MindedExample.Domain.Category>>>> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IQueryResponse<IEnumerable<MindedExample.Domain.Category>>> HandleAsync(GetCategoriesQuery query, CancellationToken cancellationToken = default)
        {
            List<MindedExample.Domain.Category> result = await query.ApplyQueryTo(_context.Categories.AsQueryable()).ToListAsync(cancellationToken);
            var response = new QueryResponse<IEnumerable<MindedExample.Domain.Category>>(result);

            return response;
        }
    } 
}
