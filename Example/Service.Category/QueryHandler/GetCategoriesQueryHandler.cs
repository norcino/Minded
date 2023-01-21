using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Common;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minded.Framework.CQRS.Query;
using Service.Category.Query;

namespace Service.Category.QueryHandler
{
    public class GetCategoriesQueryHandler : IQueryHandler<GetCategoriesQuery, List<Data.Entity.Category>>
    {
        private readonly IMindedExampleContext _context;
        private readonly ILogger<IQueryHandler<GetCategoriesQuery, List<Data.Entity.Category>>> _logger;

        public GetCategoriesQueryHandler(IMindedExampleContext context, ILogger<IQueryHandler<GetCategoriesQuery, List<Data.Entity.Category>>> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Data.Entity.Category>> HandleAsync(GetCategoriesQuery query)
        {
            return new List<Data.Entity.Category> {
                new Data.Entity.Category { Id = 1, Active = true, Description = "a" },
                new Data.Entity.Category { Id = 2, Active = true, Description = "b" },
            };// await query.ApplyTo(_context.Categories.AsQueryable()).ToListAsync();
        }
    } 
}
