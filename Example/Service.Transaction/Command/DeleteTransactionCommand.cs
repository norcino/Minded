using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace Service.Transaction.Command
{
    /// <summary>
    /// Command to delete a transaction by ID.
    /// Implements ILoggable for automatic logging through the logging decorator.
    /// Uses ValidateCommand attribute to ensure the transaction exists before deletion.
    /// </summary>
    [ValidateCommand]
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

        public object[] LoggingParameters => new object[] { TransactionId };
    }
}

