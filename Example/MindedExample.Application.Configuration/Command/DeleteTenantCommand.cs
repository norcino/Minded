using System;
using Minded.Extensions.Authorization.Attributes;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.Configuration.Command
{
    /// <summary>
    /// Command to delete a tenant and all tenant-scoped data.
    /// Requires the caller to be a global administrator.
    /// </summary>
    [ValidateCommand]
    [RequireClaim("is_global_admin", "true")]
    public class DeleteTenantCommand : ICommand, ILoggable
    {
        public DeleteTenantCommand(int tenantId, string confirmationName, Guid? traceId = null)
        {
            TenantId = tenantId;
            ConfirmationName = confirmationName;
            TraceId = traceId ?? TraceId;
        }

        public int TenantId { get; }

        public string ConfirmationName { get; }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "Deleting tenant {TenantId}";

        public string[] LoggingProperties => new[] { nameof(TenantId) };
    }
}
