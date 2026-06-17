using System;
using Minded.Extensions.Authorization.Attributes;
using Minded.Extensions.Logging;
using Minded.Extensions.Retry.Decorator;
using Minded.Framework.CQRS.Query;

namespace MindedExample.Application.Transaction.Query
{
    /// <summary>
    /// Query to retrieve a single transaction by ID.
    /// Uses [RetryQuery] to automatically retry on transient database failures.
    /// </summary>
    [RetryQuery]
    [RequireClaim("is_global_admin", "false")]
    public class GetTransactionByIdQuery : IQuery<MindedExample.Domain.Transaction>, ILoggable
    {
        public int TransactionId { get; }

        public GetTransactionByIdQuery(int transactionId, Guid? traceId = null)
        {
            TransactionId = transactionId;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();
        public string LoggingTemplate => "TransactionId: {TransactionId}";
        public string[] LoggingProperties => [nameof(TransactionId)];
    }
}