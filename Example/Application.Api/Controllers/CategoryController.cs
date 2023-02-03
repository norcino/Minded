using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Data.Context;
using Data.Entity;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.WebApi;
using Minded.Framework.Mediator;
using Service.Category.Command;
using Service.Category.Query;
using Microsoft.AspNet.OData;
//using Minded.Extensions.OData;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    public class CategoryController : BaseController
    {
        private readonly IMindedExampleContext _context;
        private readonly IMediator _mediator;
        private readonly IRestMediator _restMediator;

        public CategoryController(IMindedExampleContext context, IMediator mediator, IRestMediator restMediator)
        {
            _context = context;
            _mediator = mediator;
            _restMediator = restMediator;
        }

        public IEnumerable<Category> Get(ODataQueryOptions<Category> queryOptions)
        {
            var count = queryOptions.Count;
            var filter = queryOptions.Filter;
            var orderBy = queryOptions.OrderBy;
            var skip = queryOptions.Skip;
            var top = queryOptions.Top;
            var selectExpand = queryOptions.SelectExpand;

            var querySetting = new ODataQuerySettings
            {
                EnableConstantParameterization = true,
                EnsureStableOrdering = true,
                EnableCorrelatedSubqueryBuffering = true,
                HandleNullPropagation = HandleNullPropagationOption.Default,
                HandleReferenceNavigationPropertyExpandFilter = false,
                PageSize = 100
            };

            var queryable = _context.Categories.AsQueryable();

            var q = filter.ApplyTo(queryable, querySetting);
            q = orderBy.ApplyTo(q, querySetting);
            q = skip.ApplyTo(q, querySetting);
            q = top.ApplyTo(q, querySetting);

            if(selectExpand != null)
            {
                var sax = selectExpand.ApplyTo(q, querySetting);

                foreach (var item in sax)
                {
                    var xxxx = 0;
                }
            }
            

            var results = q.ToDynamicList<Category>();
            var countValue = count?.GetEntityCount(q);

            // OK return _context.Categories.Include(c => c.Transactions).ToList();

            //var xx = _context.Categories.ApplyODataQueryOptions(queryOptions);
            //return xx;
            var x = queryOptions.ApplyTo(_context.Categories);
            var y = x.ToDynamicList<Category>();
            return y;
            // Cast<Category>().ToList();
            //return (queryOptions.ApplyTo(_context.Categories) as IQueryable<Category>).ToList();

            //var query = ApplyODataQueryConditions<Category, GetCategoriesQuery>(queryOptions, new GetCategoriesQuery());
            //return await _mediator.ProcessQueryAsync(query);
        }
        
        [HttpGet("{id}", Name = "GetCategoryById")]
        public async Task<ActionResult> Get(int id)
        {
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetSingle, new GetCategoryByIdQuery(id));
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync([FromBody] Category category)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.Create, new CreateCategoryCommand(category));
        }
    }
}
