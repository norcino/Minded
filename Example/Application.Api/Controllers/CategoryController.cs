using System.Threading;
using System.Threading.Tasks;
using Data.Entity;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Minded.Extensions.WebApi;
using Service.Category.Command;
using Service.Category.Query;
using Minded.Extensions.CQRS.OData;

namespace Application.Api.Controllers
{
    /// <summary>
    /// Controller for managing categories.
    /// Demonstrates RestMediator usage with CQRS pattern, OData support, and proper CancellationToken handling.
    /// </summary>
    [Route("api/[controller]")]
    public class CategoryController : Controller
    {
        private readonly IRestMediator _restMediator;

        public CategoryController(IRestMediator restMediator)
        {
            _restMediator = restMediator;
        }

        /// <summary>
        /// Gets all categories with optional OData query parameters.
        /// Supports $filter, $orderby, $top, $skip, $count, and $expand.
        /// </summary>
        /// <param name="queryOptions">OData query options</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>List of categories matching the query</returns>
        [HttpGet]
        public async Task<IActionResult> Get(ODataQueryOptions<Category> queryOptions, CancellationToken cancellationToken = default)
        {
            var query = new GetCategoriesQuery();
            query.ApplyODataQueryOptions(queryOptions);
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query, cancellationToken);
        }

        /// <summary>
        /// Gets a single category by ID.
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>Category if found, 404 if not found</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetSingle, new GetCategoryByIdQuery(id), cancellationToken);
        }

        /// <summary>
        /// Creates a new category.
        /// </summary>
        /// <param name="category">Category to create</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>Created category with 201 status code</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Category category, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, new CreateCategoryCommand(category), cancellationToken);
        }

        /// <summary>
        /// Updates an existing category.
        /// </summary>
        /// <param name="id">Category ID to update</param>
        /// <param name="category">Updated category data</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>Updated category with 200 status code, or 404 if not found</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Category category, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.UpdateWithContent, new UpdateCategoryCommand(id, category), cancellationToken);
        }

        /// <summary>
        /// Deletes a category by ID.
        /// </summary>
        /// <param name="id">Category ID to delete</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>204 No Content if successful, 404 if not found</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.Delete, new DeleteCategoryCommand(id), cancellationToken);
        }
    }
}
