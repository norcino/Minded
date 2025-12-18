using System;
using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Query;

namespace Service.Transaction.Query
{
 /// <summary>
 /// Query that returns true if the Transaction with the specified Id exists, false otherwise.
 /// </summary>
 public class ExistsTransactionByIdQuery : IQuery<bool>, ILoggable
 {
 public int TransactionId { get; }

 public ExistsTransactionByIdQuery(int transactionId, Guid? traceId = null)
 {
 TransactionId = transactionId;
 TraceId = traceId ?? TraceId;
 }

 public Guid TraceId { get; } = Guid.NewGuid();

 public string LoggingTemplate => "TransactionId: {TransactionId}";

 public string[] LoggingProperties => new[] { nameof(TransactionId) };
 }
}
