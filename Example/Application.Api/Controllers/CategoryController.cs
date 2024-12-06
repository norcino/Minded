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

        [HttpGet]
        public async Task<IActionResult> Get(ODataQueryOptions<Category> queryOptions)
        {
            var query = new GetCategoriesQuery();
            query.ApplyODataQueryOptions(queryOptions);
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetSingle, new GetCategoryByIdQuery(id));
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Category category)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, new CreateCategoryCommand(category));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.Delete, new DeleteCategoryCommand(id));
        }
    }
}
