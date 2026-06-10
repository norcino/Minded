using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Extensions.Authorization.Attributes;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.Transaction.Command
{
    /// <summary>
    /// Command to update an existing transaction.
    /// Implements ILoggable for automatic logging through the logging decorator.
    /// Uses ValidateCommand attribute to ensure the transaction exists before update.
    /// </summary>
    [ValidateCommand]
    [RequirePermissions(MindedExample.Domain.Permissions.CanUpdateTransaction)]
    [RequireClaim("is_global_admin", "false")]
    public class UpdateTransactionCommand : ICommand<MindedExample.Domain.Transaction>, ILoggable
    {
        public int TransactionId { get; set; }
        public MindedExample.Domain.Transaction Transaction { get; set; }

        public UpdateTransactionCommand(int id, MindedExample.Domain.Transaction transaction, Guid? traceId = null)
        {
            TransactionId = id;
            Transaction = transaction;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "Transaction Id: {TransactionId} Credit: {Credit} Debit: {Debit} CategoryId: {CategoryId}";

        public string[] LoggingProperties => [nameof(TransactionId), "Transaction.Credit", "Transaction.Debit", "Transaction.CategoryId"];
    }
}