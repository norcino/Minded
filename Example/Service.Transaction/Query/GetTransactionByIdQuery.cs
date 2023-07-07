using System;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;

namespace Service.Transaction.Query
{
    public class GetTransactionByIdQuery : IQuery<Data.Entity.Transaction>, ILoggable
    {
        public int TransactionId { get; }
        public int CategoryId { get; }

        public GetTransactionByIdQuery(int transactionId, Guid? traceId = null)
        {
            TransactionId = transactionId;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();
        public string LoggingTemplate => "TransactionId: {TransactionId}";
        public object[] LoggingParameters => new object[] { TransactionId };
    }
}
