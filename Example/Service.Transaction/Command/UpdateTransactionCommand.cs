using System;
using Minded.Extensions.Logging;

namespace Service.Transaction.Command
{
    public class UpdateTransactionCommand : ILoggableCommand
    {
        public int TransactionId { get; set; }
        public Data.Entity.Transaction Transaction { get; set; }
        public Guid TraceId { get; } = Guid.NewGuid();

        public UpdateTransactionCommand(int id, Data.Entity.Transaction transaction, Guid? traceId = null)
        {
            TransactionId = id;
            Transaction = transaction;
            TraceId = traceId ?? TraceId;
        }

        public LogData ToLog() => new(TraceId,
            "Transaction Id: {TransactionId} Credit: {Credit} Debit: {Debit} CategoryId: {CategoryId}",
            TransactionId, Transaction.Credit, Transaction.Debit, Transaction.CategoryId);
    }
}
