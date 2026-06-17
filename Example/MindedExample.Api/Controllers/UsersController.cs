using System.Threading;
using System.Threading.Tasks;
using MindedExample.Domain;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Minded.Extensions.WebApi;
using MindedExample.Application.User.Command;
using MindedExample.Application.User.Query;
using Minded.Extensions.CQRS.OData;
using Microsoft.AspNetCore.Authorization;

namespace MindedExample.Api.Controllers
{
    /// <summary>
    /// Controller for managing users within the current tenant.
    /// Access is restricted to tenant owners, admins, and users holding the TenantAdmin application role
    /// via the <c>TenantMemberManagement</c> authorization policy.
    /// User sensitive data (name, surname, email) is protected in logs by the DataProtectionLoggingSanitizer.
    /// Newly created users are automatically assigned the default role (handled by the command handler).
    /// </summary>
    [Route("api/[controller]")]
    [Authorize(Policy = "TenantMemberManagement")]
    public class UsersController : Controller
    {
        private readonly IRestMediator _restMediator;

        /// <summary>
        /// Initializes a new <see cref="UsersController"/>.
        /// </summary>
        public UsersController(IRestMediator restMediator)
        {
            _restMediator = restMediator;
        }

        /// <summary>
        /// Gets all users with optional OData query parameters.
        /// </summary>
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
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetSingle, new GetUserByIdQuery(id), cancellationToken);
        }

        /// <summary>
        /// Creates a new user within the current tenant.
        /// The default role is automatically assigned by the command handler.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] User user, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, new CreateUserCommand(user), cancellationToken);
        }

        /// <summary>
        /// Updates an existing user.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] User user, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.UpdateWithContent, new UpdateUserCommand(id, user), cancellationToken);
        }

        /// <summary>
        /// Deletes a user by ID.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.Delete, new DeleteUserCommand(id), cancellationToken);
        }
    }
}
