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
    /// Provides authentication, registration, invitation onboarding, and password reset endpoints.
    /// All business logic is delegated to application-layer command/query handlers via
    /// <see cref="IRestMediator"/>.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IRestMediator _restMediator;

        /// <summary>Initializes a new <see cref="AuthController"/>.</summary>
        public AuthController(IRestMediator restMediator)
        {
            _restMediator = restMediator;
        }

        /// <summary>Registers a new user. Supports create-tenant, join-tenant, and from-invite flows.</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var command = new RegisterCommand(
                request?.Name,
                request?.Surname,
                request?.Email,
                request?.Password,
                request?.TenantName,
                request?.Mode,
                request?.InviteToken);

            return await _restMediator.ProcessRestCommandAsync(RestOperation.ActionWithResultContent, command, cancellationToken);
        }

        /// <summary>Returns public details about a tenant invite identified by its token or short code.</summary>
        [HttpGet("invite/{token}")]
        public async Task<IActionResult> GetInviteDetails(string token, CancellationToken cancellationToken = default)
            => await _restMediator.ProcessRestQueryAsync(RestOperation.GetSingle, new GetInviteDetailsQuery(token), cancellationToken);

        /// <summary>Authenticates a user and returns a JWT access token.</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
            => await _restMediator.ProcessRestCommandAsync(
                RestOperation.ActionWithResultContent,
                new LoginCommand(request?.Email, request?.Password),
                cancellationToken);

        /// <summary>Initiates a password reset flow. Always returns 200 OK to avoid account enumeration.</summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken = default)
            => await _restMediator.ProcessRestCommandAsync(
                RestOperation.Action,
                new ForgotPasswordCommand(request?.Email),
                cancellationToken);

        /// <summary>Completes a password reset using a previously issued reset token.</summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken = default)
            => await _restMediator.ProcessRestCommandAsync(
                RestOperation.Action,
                new ResetPasswordCommand(request?.Token, request?.NewPassword),
                cancellationToken);

        /// <summary>Accepts a tenant invite and creates a new user account.</summary>
        [HttpPost("accept-invite")]
        public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request, CancellationToken cancellationToken = default)
            => await _restMediator.ProcessRestCommandAsync(
                RestOperation.ActionWithResultContent,
                new AcceptInviteCommand(request?.CodeOrToken, request?.Email, request?.Name, request?.Surname, request?.Password),
                cancellationToken);

        /// <summary>Returns the profile of the currently authenticated user.</summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me(CancellationToken cancellationToken = default)
            => await _restMediator.ProcessRestQueryAsync(RestOperation.GetSingle, new GetCurrentUserQuery(), cancellationToken);
    }
}
