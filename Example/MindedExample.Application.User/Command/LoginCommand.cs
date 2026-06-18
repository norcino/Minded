using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Command to authenticate a user with email and password credentials.
    /// On success, returns an <see cref="AuthResult"/> containing a JWT access token.
    /// On failure (wrong credentials), throws <see cref="System.Security.Authentication.InvalidCredentialException"/>,
    /// which is caught by <c>RestMediator</c> and mapped to HTTP 401.
    /// </summary>
    [ValidateCommand]
    public class LoginCommand : ICommand<AuthResult>, ILoggable
    {
        /// <summary>Gets the user's email address.</summary>
        public string Email { get; }

        /// <summary>Gets the plain-text password to verify.</summary>
        public string Password { get; }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "Login attempt for {Email}";

        /// <inheritdoc />
        public string[] LoggingProperties => [nameof(Email)];

        /// <summary>
        /// Initializes a new <see cref="LoginCommand"/>.
        /// </summary>
        public LoginCommand(string email, string password, Guid? traceId = null)
        {
            Email = email;
            Password = password;
            TraceId = traceId ?? TraceId;
        }
    }
}
