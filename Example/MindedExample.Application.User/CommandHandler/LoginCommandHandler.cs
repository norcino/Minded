using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Common;
using MindedExample.Application.User.Command;
using MindedExample.Application.User.Services;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Application.User.CommandHandler
{
    /// <summary>
    /// Handles <see cref="LoginCommand"/> by verifying the user's credentials and,
    /// on success, returning an <see cref="AuthResult"/> containing a JWT access token.
    /// Authentication failures return a failed <see cref="ICommandResponse{TResult}"/> with
    /// <see cref="GenericErrorCodes.NotAuthenticated"/> so that <c>RestMediator</c> maps them
    /// to HTTP 401 Unauthorized via <c>DefaultRestRulesProvider</c>.
    /// </summary>
    public class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResult>
    {
        private readonly IMindedExampleContext _context;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IPasswordService _passwordService;
        private readonly IAuthResultBuilder _authResultBuilder;

        /// <summary>
        /// Initializes a new <see cref="LoginCommandHandler"/>.
        /// </summary>
        public LoginCommandHandler(
            IMindedExampleContext context,
            IJwtTokenService jwtTokenService,
            IPasswordService passwordService,
            IAuthResultBuilder authResultBuilder)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _passwordService = passwordService;
            _authResultBuilder = authResultBuilder;
        }

        /// <inheritdoc />
        public async Task<ICommandResponse<AuthResult>> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
        {
            var email = command.Email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
                return InvalidCredentials();

            if (!user.IsActive)
            {
                var hasPendingRequest = await _context.TenantJoinRequests
                    .AsNoTracking()
                    .AnyAsync(r => r.Email == email && r.ProcessedAtUtc == null, cancellationToken);

                if (hasPendingRequest)
                {
                    return CommandResponse<AuthResult>.Error(
                        new OutcomeEntry(
                            "Login",
                            "Your request to join the tenant is pending approval.",
                            attemptedValue: null,
                            Severity.Error,
                            GenericErrorCodes.NotAuthenticated));
                }

                return InvalidCredentials();
            }

            if (!_passwordService.VerifyPassword(user, user.PasswordHash, command.Password))
                return InvalidCredentials();

            var token = _jwtTokenService.CreateAccessToken(user);
            var authResult = await _authResultBuilder.BuildAsync(user, token, cancellationToken);

            return CommandResponse<AuthResult>.Success(authResult);
        }

        private static ICommandResponse<AuthResult> InvalidCredentials() =>
            CommandResponse<AuthResult>.Error(
                new OutcomeEntry("Login", "Invalid credentials.", attemptedValue: null, Severity.Error, GenericErrorCodes.NotAuthenticated));
    }
}
