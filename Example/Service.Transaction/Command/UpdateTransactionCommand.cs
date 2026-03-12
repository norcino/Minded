using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace Service.Transaction.Command
{
    /// <summary>
    /// Command to update an existing transaction.
    /// Implements ILoggable for automatic logging through the logging decorator.
    /// Uses ValidateCommand attribute to ensure the transaction exists before update.
    /// </summary>
    [ValidateCommand]
    public class UpdateTransactionCommand : ICommand<Data.Entity.Transaction>, ILoggable
    {
        public int TransactionId { get; set; }
        public Data.Entity.Transaction Transaction { get; set; }

        public UpdateTransactionCommand(int id, Data.Entity.Transaction transaction, Guid? traceId = null)
        {
            TransactionId = id;
            Transaction = transaction;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "Transaction Id: {TransactionId} Credit: {Credit} Debit: {Debit} CategoryId: {CategoryId}";

        public string[] LoggingProperties => new[] { nameof(TransactionId), "Transaction.Credit", "Transaction.Debit", "Transaction.CategoryId" };
    }
}
