using System;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Command;

namespace Service.Transaction.Command
{
    public class UpdateTransactionCommand : ICommand<int>, ILoggable
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

        public object[] LoggingParameters => new object[] { TransactionId, Transaction.Credit, Transaction.Debit, Transaction.CategoryId };
    }
}
