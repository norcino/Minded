using System;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Command to create a new tenant invitation link.
    /// The optional email pre-populates the invite for a specific recipient.
    /// The frontend base URL is used to construct the full invite link returned in the response.
    /// </summary>
    public class CreateTenantInviteCommand : ICommand<TenantInviteResult>, ILoggable
    {
        /// <summary>
        /// Optional email address for the intended recipient.
        /// </summary>
        public string Email { get; }

        /// <summary>
        /// The base URL of the frontend application, used to build the invite link.
        /// </summary>
        public string FrontendBaseUrl { get; }

        /// <summary>
        /// Initializes a new <see cref="CreateTenantInviteCommand"/>.
        /// </summary>
        public CreateTenantInviteCommand(string email, string frontendBaseUrl, Guid? traceId = null)
        {
            Email = email;
            FrontendBaseUrl = frontendBaseUrl;
            TraceId = traceId ?? TraceId;
        }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "Creating tenant invite for email: {Email}";

        /// <inheritdoc />
        public string[] LoggingProperties => [nameof(Email)];
    }
}
