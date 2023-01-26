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
using Minded.Extensions.OData;

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
            // Avoid coupling with OData



            // OK return _context.Categories.Include(c => c.Transactions).ToList();

            var xx = _context.Categories.ApplyODataQueryOptions(queryOptions);
            return xx;
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
            //var result = await _mediator.ProcessQueryAsync(new GetCategoryByIdQuery(id));

            //if (result == null)
            //{
            //    return NotFound();
            //}

            //return new OkObjectResult(result);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync([FromBody] Category category)
        {
            return await _restMediator.ProcessRestCommandAsync<Category>(RestOperation.Create, new CreateCategoryCommand(category));
            //var result = await _mediator.ProcessCommandAsync<int>(new CreateCategoryCommand(category));

            //if (result.Successful)
            //{
            //    return new CreatedAtRouteResult("GetCategoryById", new {Id = result.Result}, result);
            //}

            //return StatusCode(StatusCodes.Status400BadRequest, result);
        }
    }
}
