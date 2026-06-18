using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Command to remove a user from the current tenant.
    /// </summary>
    [ValidateCommand]
    public class RemoveTenantUserCommand : ICommand, ILoggable
    {
        /// <summary>Gets the ID of the user to remove.</summary>
        public int UserId { get; }

        /// <summary>
        /// Initializes a new <see cref="RemoveTenantUserCommand"/>.
        /// </summary>
        public RemoveTenantUserCommand(int userId, Guid? traceId = null)
        {
            UserId = userId;
            TraceId = traceId ?? TraceId;
        }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "UserId: {UserId}";

        /// <inheritdoc />
        public string[] LoggingProperties => [nameof(UserId)];
    }
}
