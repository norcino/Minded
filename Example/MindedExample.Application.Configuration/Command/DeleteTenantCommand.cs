using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.Configuration.Command
{
    /// <summary>
    /// Command to delete a tenant and all tenant-scoped data.
    /// </summary>
    [ValidateCommand]
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
