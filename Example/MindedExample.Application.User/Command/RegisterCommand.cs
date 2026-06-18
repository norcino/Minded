using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Command to register a new user. Supports three registration flows:
    /// <list type="bullet">
    ///   <item>Create-tenant: registers a new user as the owner of a newly created tenant (default).</item>
    ///   <item>Join-tenant: creates a pending join request for an existing tenant.</item>
    ///   <item>From-invite: registers a new user into an existing tenant via an invite token.</item>
    /// </list>
    /// </summary>
    [ValidateCommand]
    public class RegisterCommand : ICommand<AuthResult>, ILoggable
    {
        /// <summary>Gets the user's first name.</summary>
        public string Name { get; }

        /// <summary>Gets the user's surname.</summary>
        public string Surname { get; }

        /// <summary>Gets the user's email address.</summary>
        public string Email { get; }

        /// <summary>Gets the plain-text password.</summary>
        public string Password { get; }

        /// <summary>Gets the tenant name (required for create-tenant and join-tenant modes).</summary>
        public string TenantName { get; }

        /// <summary>
        /// Gets the registration mode. Supported values: <c>create-tenant</c> (default), <c>join-tenant</c>.
        /// </summary>
        public string Mode { get; }

        /// <summary>Gets the invite token or code when registering from an invite.</summary>
        public string InviteToken { get; }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "Register user {Email} mode {Mode}";

        /// <inheritdoc />
        public string[] LoggingProperties => [nameof(Email), nameof(Mode)];

        /// <summary>
        /// Initializes a new <see cref="RegisterCommand"/>.
        /// </summary>
        public RegisterCommand(
            string name,
            string surname,
            string email,
            string password,
            string tenantName = null,
            string mode = null,
            string inviteToken = null,
            Guid? traceId = null)
        {
            Name = name;
            Surname = surname;
            Email = email;
            Password = password;
            TenantName = tenantName;
            Mode = mode;
            InviteToken = inviteToken;
            TraceId = traceId ?? TraceId;
        }
    }
}
