using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.User.Command
{
    /// <summary>
    /// Command to update the tenant role of a user within the current tenant.
    /// </summary>
    [ValidateCommand]
    public class UpdateTenantUserRoleCommand : ICommand, ILoggable
    {
        /// <summary>Gets the ID of the user whose role is being updated.</summary>
        public int UserId { get; }

        /// <summary>Gets the new tenant role to assign.</summary>
        public string Role { get; }

        /// <summary>
        /// Initializes a new <see cref="UpdateTenantUserRoleCommand"/>.
        /// </summary>
        public UpdateTenantUserRoleCommand(int userId, string role, Guid? traceId = null)
        {
            UserId = userId;
            Role = role;
            TraceId = traceId ?? TraceId;
        }

        /// <inheritdoc />
        public Guid TraceId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public string LoggingTemplate => "UserId: {UserId}, Role: {Role}";

        /// <inheritdoc />
        public string[] LoggingProperties => [nameof(UserId), nameof(Role)];
    }
}
