using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minded.Framework.CQRS.Query;
using Service.Category.Query;

namespace Service.Category.QueryHandler
{
    public class GetCategoriesQueryHandler : IQueryHandler<GetCategoriesQuery, IQueryResponse<IEnumerable<Data.Entity.Category>>>
    {
        private readonly IMindedExampleContext _context;
        private readonly ILogger<IQueryHandler<GetCategoriesQuery, IQueryResponse<IEnumerable<Data.Entity.Category>>>> _logger;

        public GetCategoriesQueryHandler(IMindedExampleContext context, ILogger<IQueryHandler<GetCategoriesQuery, IQueryResponse<IEnumerable<Data.Entity.Category>>>> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IQueryResponse<IEnumerable<Data.Entity.Category>>> HandleAsync(GetCategoriesQuery query)
        {
            var result = await query.ApplyQueryTo(_context.Categories.AsQueryable()).ToListAsync();
            var response = new QueryResponse<IEnumerable<Data.Entity.Category>>(result);

            return response;
        }
    } 
}
