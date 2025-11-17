using System;
using TransactionScopeOption = System.Transactions.TransactionScopeOption;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace Minded.Extensions.Transaction.Configuration
{
    /// <summary>
    /// Configuration options for the transaction decorator.
    /// Controls transaction behavior for commands and queries.
    /// </summary>
    public class TransactionOptions
    {
        /// <summary>
        /// Gets or sets the default transaction scope option when not specified in attribute.
        /// Determines how the transaction scope participates in ambient transactions.
        /// Default: TransactionScopeOption.Required (joins existing transaction or creates new one)
        /// </summary>
        public TransactionScopeOption DefaultTransactionScopeOption { get; set; } = TransactionScopeOption.Required;

        /// <summary>
        /// Gets or sets the default isolation level when not specified in attribute.
        /// Controls the locking behavior and consistency guarantees of the transaction.
        /// Default: IsolationLevel.ReadCommitted (prevents dirty reads, allows non-repeatable reads)
        /// </summary>
        public IsolationLevel DefaultIsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

        /// <summary>
        /// Gets or sets the default transaction timeout.
        /// Transactions exceeding this duration will be automatically rolled back.
        /// Default: 1 minute
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets whether to automatically roll back the transaction when ICommandResponse.Successful is false.
        /// When true, unsuccessful command responses will not call scope.Complete(), causing rollback.
        /// When false, only exceptions will cause rollback.
        /// Default: true
        /// </summary>
        public bool RollbackOnUnsuccessfulResponse { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log transaction start/complete/rollback events.
        /// When true, transaction lifecycle events are logged at Information level.
        /// Default: true
        /// </summary>
        public bool EnableLogging { get; set; } = true;
    }
}

