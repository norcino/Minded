using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Command to approve a pending tenant join request, creating the user account.
    /// </summary>
    [ValidateCommand]
    public class ApproveTenantJoinRequestCommand : ICommand, ILoggable
    {
        /// <summary>Gets the ID of the join request to approve.</summary>
        public int RequestId { get; }

        /// <summary>
        /// Initializes a new <see cref="ApproveTenantJoinRequestCommand"/>.
        /// </summary>
        public ApproveTenantJoinRequestCommand(int requestId, Guid? traceId = null)
        {
            RequestId = requestId;
            TraceId = traceId ?? TraceId;
        }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "RequestId: {RequestId}";

        /// <inheritdoc />
        public string[] LoggingProperties => [nameof(RequestId)];
    }
}
