using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Command to accept a tenant invite and create a user account.
    /// The invite is identified by either its token (URL link) or short code.
    /// Returns an <see cref="AuthResult"/> on success so the new member can immediately log in.
    /// </summary>
    [ValidateCommand]
    public class AcceptInviteCommand : ICommand<AuthResult>, ILoggable
    {
        /// <summary>Gets the invite token or short code.</summary>
        public string CodeOrToken { get; }

        /// <summary>Gets the new user's email address.</summary>
        public string Email { get; }

        /// <summary>Gets the new user's first name.</summary>
        public string Name { get; }

        /// <summary>Gets the new user's surname.</summary>
        public string Surname { get; }

        /// <summary>Gets the plain-text password for the new user account.</summary>
        public string Password { get; }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "AcceptInvite for {Email}";

        /// <inheritdoc />
        public string[] LoggingProperties => [nameof(Email)];

        /// <summary>
        /// Initializes a new <see cref="AcceptInviteCommand"/>.
        /// </summary>
        public AcceptInviteCommand(
            string codeOrToken,
            string email,
            string name,
            string surname,
            string password,
            Guid? traceId = null)
        {
            CodeOrToken = codeOrToken;
            Email = email;
            Name = name;
            Surname = surname;
            Password = password;
            TraceId = traceId ?? TraceId;
        }
    }
}
