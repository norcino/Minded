﻿using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using Service.Category.Query;

namespace Service.Category.QueryHandler
{
    public class GetCategoryByIdQueryHandler : IQueryHandler<GetCategoryByIdQuery, Data.Entity.Category>
    {
        private readonly IMindedExampleContext _context;

        public GetCategoryByIdQueryHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<Data.Entity.Category> HandleAsync(GetCategoryByIdQuery query)
        {
            return await _context.Categories.SingleOrDefaultAsync(c => c.Id == query.CategoryId);
        }
    }
}
