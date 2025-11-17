using System;
using System.Transactions;

namespace Minded.Extensions.Transaction.Decorator
{
    /// <summary>
    /// Marks a command to execute within a database transaction.
    /// All database operations within the command handler, including nested commands/queries invoked via IMediator,
    /// will execute within the same transaction scope and can be rolled back on error.
    ///
    /// WARNING: This transaction does NOT cover:
    /// - Remote service calls (HTTP, gRPC, etc.)
    /// - Message queue operations (RabbitMQ, Azure Service Bus, etc.)
    /// - File system operations
    /// - External API calls
    ///
    /// For distributed scenarios, use Saga pattern or Transactional Outbox pattern.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TransactionCommandAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the transaction scope option.
        /// Determines how this transaction participates in ambient transactions.
        /// Default: TransactionScopeOption.Required (joins existing transaction or creates new one)
        /// </summary>
        public TransactionScopeOption TransactionScopeOption { get; set; }

        /// <summary>
        /// Gets or sets the isolation level for the transaction.
        /// Controls locking behavior and consistency guarantees.
        /// Default: IsolationLevel.ReadCommitted (prevents dirty reads)
        /// </summary>
        public IsolationLevel IsolationLevel { get; set; }

        /// <summary>
        /// Gets or sets the transaction timeout in seconds.
        /// Transactions exceeding this duration will be automatically rolled back.
        /// Use 0 to use the default timeout configured in TransactionOptions.
        /// Default: 0 (use default timeout)
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// Initializes a new instance of the TransactionCommandAttribute class with default values.
        /// </summary>
        public TransactionCommandAttribute()
        {
            TransactionScopeOption = TransactionScopeOption.Required;
            IsolationLevel = IsolationLevel.ReadCommitted;
            TimeoutSeconds = 0; // Use default
        }
    }
}
