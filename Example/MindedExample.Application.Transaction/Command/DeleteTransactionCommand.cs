using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Extensions.Authorization.Attributes;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.Transaction.Command
{
    /// <summary>
    /// Command to delete a transaction by ID.
    /// Implements ILoggable for automatic logging through the logging decorator.
    /// Uses ValidateCommand attribute to ensure the transaction exists before deletion.
    /// </summary>
    [ValidateCommand]
    [RequirePermissions(MindedExample.Domain.Permissions.CanDeleteTransaction)]
    [RequireClaim("is_global_admin", "false")]
    public class DeleteTransactionCommand : ICommand, ILoggable
    {
        public int TransactionId { get; }

        public DeleteTransactionCommand(int id, Guid? traceId = null)
        {
            TransactionId = id;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "TransactionId: {TransactionId}";

        public string[] LoggingProperties => [nameof(TransactionId)];
    }
}

