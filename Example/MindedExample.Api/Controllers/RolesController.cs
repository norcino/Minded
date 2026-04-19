using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Minded.Extensions.WebApi;
using MindedExample.Application.Role.Command;
using MindedExample.Application.Role.Query;

namespace MindedExample.Api.Controllers
{
    /// <summary>
    /// Controller for managing roles and permissions.
    /// Provides CRUD operations for roles, permission listing, and role-permission assignment.
    /// </summary>
    [Route("api/[controller]")]
    public class RolesController : Controller
    {
        private readonly IRestMediator _restMediator;

        public RolesController(IRestMediator restMediator)
        {
            _restMediator = restMediator;
        }

        /// <summary>
        /// Gets all roles with their permissions.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, new GetRolesQuery(), cancellationToken);
        }

        /// <summary>
        /// Creates a new role.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, new CreateRoleCommand(request.Name), cancellationToken);
        }

        /// <summary>
        /// Deletes a role by name.
        /// </summary>
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.Delete, new DeleteRoleCommand(name), cancellationToken);
        }

        /// <summary>
        /// Gets all available permissions.
        /// </summary>
        [HttpGet("/api/permissions")]
        public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, new GetPermissionsQuery(), cancellationToken);
        }

        /// <summary>
        /// Updates the permissions assigned to a role.
        /// </summary>
        [HttpPut("{name}/permissions")]
        public async Task<IActionResult> UpdatePermissions(string name, [FromBody] List<string> permissionNames, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.UpdateWithContent, new UpdateRolePermissionsCommand(name, permissionNames), cancellationToken);
        }

        /// <summary>
        /// Assigns roles to a user. Replaces all existing role assignments.
        /// </summary>
        [HttpPut("/api/users/{userId}/roles")]
        public async Task<IActionResult> AssignRolesToUser(int userId, [FromBody] List<string> roleNames, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.UpdateWithContent, new AssignRolesToUserCommand(userId, roleNames), cancellationToken);
        }

        /// <summary>
        /// Gets all users with their assigned roles.
        /// </summary>
        [HttpGet("/api/users-with-roles")]
        public async Task<IActionResult> GetUsersWithRoles(CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, new GetUsersWithRolesQuery(), cancellationToken);
        }

        /// <summary>
        /// Resets all role permissions to their default values.
        /// </summary>
        [HttpPost("reset-to-default")]
        public async Task<IActionResult> ResetToDefault(CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, new ResetRolesToDefaultCommand(), cancellationToken);
        }
    }

    public class CreateRoleRequest
    {
        public string Name { get; set; }
    }
}
