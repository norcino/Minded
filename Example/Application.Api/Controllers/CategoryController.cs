using System.Threading.Tasks;
using Data.Entity;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Minded.Extensions.WebApi;
using Service.Category.Command;
using Service.Category.Query;
using Minded.Extensions.CQRS.OData;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    public class CategoryController : Controller
    {
        private readonly IRestMediator _restMediator;

        public CategoryController(IRestMediator restMediator)
        {
            _restMediator = restMediator;
        }

        public async Task<ActionResult> Get(ODataQueryOptions<Category> queryOptions)
        {
            var query = new GetCategoriesQuery();
            query.ApplyODataQueryOptions(queryOptions);
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query);
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
