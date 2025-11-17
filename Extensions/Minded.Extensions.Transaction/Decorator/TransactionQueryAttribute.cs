using System;
using System.Transactions;

namespace Minded.Extensions.Transaction.Decorator
{
    /// <summary>
    /// Marks a query to execute within a database transaction.
    /// Typically used for queries requiring consistent snapshot across multiple tables
    /// or queries with specific isolation level requirements (e.g., Snapshot, Serializable).
    ///
    /// NOTE: Most read-only queries do NOT need transactions. Consider using database-level
    /// snapshot isolation instead of explicit transactions for better performance.
    ///
    /// Use cases for query transactions:
    /// - Queries requiring consistent snapshot across multiple tables
    /// - Queries with IsolationLevel.Snapshot or IsolationLevel.Serializable
    /// - Queries that perform temporary table operations
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TransactionQueryAttribute : Attribute
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
        /// Initializes a new instance of the TransactionQueryAttribute class with default values.
        /// </summary>
        public TransactionQueryAttribute()
        {
            TransactionScopeOption = TransactionScopeOption.Required;
            IsolationLevel = IsolationLevel.ReadCommitted;
            TimeoutSeconds = 0; // Use default
        }
    }
}
