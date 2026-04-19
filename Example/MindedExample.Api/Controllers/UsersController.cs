using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Domain;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Minded.Extensions.WebApi;
using Minded.Framework.Mediator;
using MindedExample.Application.User.Command;
using MindedExample.Application.User.Query;
using MindedExample.Application.Role.Command;
using Minded.Extensions.CQRS.OData;

namespace MindedExample.Api.Controllers
{
    /// <summary>
    /// Controller for managing users.
    /// Demonstrates RestMediator usage with CQRS pattern, OData support, and proper CancellationToken handling.
    /// User's sensitive data (name, surname, email) is protected in logs by the DataProtectionLoggingSanitizer.
    /// Newly created users are automatically assigned the default role.
    /// </summary>
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly IRestMediator _restMediator;
        private readonly IMediator _mediator;

        public UsersController(IRestMediator restMediator, IMediator mediator)
        {
            _restMediator = restMediator;
            _mediator = mediator;
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
        /// Creates a new user.
        /// Automatically assigns the default role to newly created users.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] User user, CancellationToken cancellationToken = default)
        {
            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, new CreateUserCommand(user), cancellationToken);

            // Assign default role to newly created users
            if (user.Id > 0)
            {
                await _mediator.ProcessCommandAsync(
                    new AssignRolesToUserCommand(user.Id, new List<string> { DefaultRolesDefinition.DefaultRole }),
                    cancellationToken);
            }

            return result;
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
