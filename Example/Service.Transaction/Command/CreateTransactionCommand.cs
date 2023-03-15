using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;

namespace Service.Transaction.Command
{
    [ValidateCommand]
    public class CreateTransactionCommand : ILoggableCommand<int>
    {
        public Data.Entity.Transaction Transaction { get; set; }
        public Guid TraceId { get; } = Guid.NewGuid();

        public CreateTransactionCommand(Data.Entity.Transaction transaction, Guid? traceId = null)
        {
            Transaction = transaction;
            TraceId = traceId ?? TraceId;
        }
        public LogData ToLog() => new(TraceId, "Credit: {Credit} Debit: {Debit} CategoryId: {CategoryId}", Transaction.Credit, Transaction.Debit, Transaction.CategoryId);
    }
}
