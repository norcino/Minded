using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Entity;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Minded.Framework.Mediator;
using Service.Category.Command;
using Service.Category.Query;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    public class CategoryController : BaseController
    {
        private readonly IMediator _mediator;

        public CategoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public Task<List<Category>> Get(ODataQueryOptions<Category> queryOptions)
        {
            var query = ApplyODataQueryConditions<Category, GetCategoriesQuery>(queryOptions, new GetCategoriesQuery());
            return _mediator.ProcessQueryAsync(query);
        }
        
        [HttpGet("{id}", Name = "GetCategoryById")]
        public async Task<ActionResult> Get(int id)
        {
            // Create 201 Created - 400 Bad request
            // Update 200 Ok - 400 Bad request
            // Delete 404  NotFound - 200 Ok
            // Patch 200 Ok - 400 Bad request
            // Get 404 Not found - 200 Ok
            var result = await _mediator.ProcessQueryAsync(new GetCategoryByIdQuery(id));

            if (result == null)
            {
                return NotFound();
            }

            return new OkObjectResult(result);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync([FromBody] Category category)
        {
            //            return await _mediator.ProcessRestQueryAsync(HttpMethod.Get, query);
            var result = await _mediator.ProcessCommandAsync<int>(new CreateCategoryCommand(category));

            if (result.Successful)
            {
                return new CreatedAtRouteResult("GetCategoryById", new {Id = result.Result}, result);
            }

            return StatusCode(StatusCodes.Status400BadRequest, result);
        }
    }
}
