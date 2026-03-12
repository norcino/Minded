using System.Threading;
using System.Threading.Tasks;
using Data.Entity;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Minded.Extensions.WebApi;
using Service.User.Command;
using Service.User.Query;
using Minded.Extensions.CQRS.OData;

namespace Application.Api.Controllers
{
    /// <summary>
    /// Controller for managing users.
    /// Demonstrates RestMediator usage with CQRS pattern, OData support, and proper CancellationToken handling.
    /// User's sensitive data (name, surname, email) is protected in logs by the DataProtectionLoggingSanitizer.
    /// </summary>
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly IRestMediator _restMediator;

        public UsersController(IRestMediator restMediator)
        {
            _restMediator = restMediator;
        }

        /// <summary>
        /// Gets all users with optional OData query parameters.
        /// Supports $filter, $orderby, $top, $skip, $count, and $expand.
        /// </summary>
        /// <param name="queryOptions">OData query options</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>List of users matching the query</returns>
        [HttpGet]
        public async Task<IActionResult> Get(ODataQueryOptions<User> queryOptions, CancellationToken cancellationToken = default)
        {
            var query = new GetUsersQuery();
            query.ApplyODataQueryOptions(queryOptions);
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query, cancellationToken);
        }

        /// <summary>
        /// Gets a single user by ID.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>User if found, 404 if not found</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetSingle, new GetUserByIdQuery(id), cancellationToken);
        }

        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="user">User to create</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>Created user with 201 status code</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] User user, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, new CreateUserCommand(user), cancellationToken);
        }

        /// <summary>
        /// Updates an existing user.
        /// </summary>
        /// <param name="id">User ID to update</param>
        /// <param name="user">Updated user data</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>Updated user with 200 status code, or 404 if not found</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] User user, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.UpdateWithContent, new UpdateUserCommand(id, user), cancellationToken);
        }

        /// <summary>
        /// Deletes a user by ID.
        /// </summary>
        /// <param name="id">User ID to delete</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>204 No Content if successful, 404 if not found</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.Delete, new DeleteUserCommand(id), cancellationToken);
        }
    }
}

