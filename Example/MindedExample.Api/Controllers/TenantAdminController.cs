using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minded.Extensions.WebApi;
using MindedExample.Api.Models;
using MindedExample.Application.User.Command;
using MindedExample.Application.User.Query;

namespace MindedExample.Api.Controllers
{
    /// <summary>
    /// Tenant administration endpoints for invites, member role updates, and member removal.
    /// Access is restricted to tenant Owners, Admins, and users with the TenantAdmin role.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "TenantMemberManagement")]
    [Route("api/tenant-admin")]
    public class TenantAdminController : ControllerBase
    {
        private readonly IRestMediator _restMediator;

        /// <summary>
        /// Initializes a new <see cref="TenantAdminController"/>.
        /// </summary>
        public TenantAdminController(IRestMediator restMediator)
        {
            _restMediator = restMediator;
        }

        /// <summary>
        /// Returns all users belonging to the current tenant.
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestQueryAsync(
                RestOperation.GetMany, new GetTenantAdminUsersQuery(), cancellationToken);
        }

        /// <summary>
        /// Creates a new tenant invitation link.
        /// </summary>
        [HttpPost("invites")]
        public async Task<IActionResult> CreateInvite(
            [FromBody] CreateInviteRequest request, CancellationToken cancellationToken = default)
        {
            var frontendBase = Request.Headers["Origin"].FirstOrDefault()
                ?? $"{Request.Scheme}://{Request.Host}";

            return await _restMediator.ProcessRestCommandAsync(
                RestOperation.CreateWithContent,
                new CreateTenantInviteCommand(request?.Email, frontendBase),
                cancellationToken);
        }

        /// <summary>
        /// Updates the tenant role of an existing member.
        /// </summary>
        [HttpPut("users/{userId}/role")]
        public async Task<IActionResult> UpdateUserRole(
            int userId, [FromBody] UpdateTenantUserRoleRequest request, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(
                RestOperation.UpdateWithContent,
                new UpdateTenantUserRoleCommand(userId, request?.Role),
                cancellationToken);
        }

        /// <summary>
        /// Removes a member from the current tenant.
        /// </summary>
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> RemoveUser(int userId, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(
                RestOperation.Delete,
                new RemoveTenantUserCommand(userId),
                cancellationToken);
        }

        /// <summary>
        /// Returns all pending join requests for the current tenant.
        /// </summary>
        [HttpGet("join-requests")]
        public async Task<IActionResult> GetPendingJoinRequests(CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestQueryAsync(
                RestOperation.GetMany, new GetTenantJoinRequestsQuery(), cancellationToken);
        }

        /// <summary>
        /// Approves a pending join request, creating the user account.
        /// </summary>
        [HttpPost("join-requests/{requestId:int}/approve")]
        public async Task<IActionResult> ApproveJoinRequest(int requestId, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(
                RestOperation.UpdateWithContent,
                new ApproveTenantJoinRequestCommand(requestId),
                cancellationToken);
        }

        /// <summary>
        /// Rejects a pending join request.
        /// </summary>
        [HttpPost("join-requests/{requestId:int}/reject")]
        public async Task<IActionResult> RejectJoinRequest(int requestId, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(
                RestOperation.UpdateWithContent,
                new RejectTenantJoinRequestCommand(requestId),
                cancellationToken);
        }
    }
}
