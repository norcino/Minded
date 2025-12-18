using System;
using TransactionScopeOption = System.Transactions.TransactionScopeOption;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace Minded.Extensions.Transaction.Configuration
{
    /// <summary>
    /// Configuration options for the transaction decorator.
    /// Controls transaction behavior for commands and queries.
    /// All properties support both static values and dynamic providers for runtime configuration (e.g., feature flags).
    /// </summary>
    public class TransactionOptions
    {
        /// <summary>
        /// Gets or sets the default transaction scope option when not specified in attribute.
        /// Determines how the transaction scope participates in ambient transactions.
        /// This property is used as the default value when DefaultTransactionScopeOptionProvider is not set.
        /// Default: TransactionScopeOption.Required (joins existing transaction or creates new one)
        /// </summary>
        public TransactionScopeOption DefaultTransactionScopeOption { get; set; } = TransactionScopeOption.Required;

        /// <summary>
        /// Gets or sets a function that dynamically determines the default transaction scope option.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over DefaultTransactionScopeOption.
        /// The function is called each time a transaction is initiated.
        /// Example: () => _configService.GetValue("transaction-scope-option", TransactionScopeOption.Required)
        /// Default: null (uses DefaultTransactionScopeOption instead)
        /// </summary>
        public Func<TransactionScopeOption> DefaultTransactionScopeOptionProvider { get; set; }

        /// <summary>
        /// Gets or sets the default isolation level when not specified in attribute.
        /// Controls the locking behavior and consistency guarantees of the transaction.
        /// This property is used as the default value when DefaultIsolationLevelProvider is not set.
        /// Default: IsolationLevel.ReadCommitted (prevents dirty reads, allows non-repeatable reads)
        /// </summary>
        public IsolationLevel DefaultIsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

        /// <summary>
        /// Gets or sets a function that dynamically determines the default isolation level.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over DefaultIsolationLevel.
        /// The function is called each time a transaction is initiated.
        /// Example: () => _configService.GetValue("transaction-isolation-level", IsolationLevel.ReadCommitted)
        /// Default: null (uses DefaultIsolationLevel instead)
        /// </summary>
        public Func<IsolationLevel> DefaultIsolationLevelProvider { get; set; }

        /// <summary>
        /// Gets or sets the default transaction timeout.
        /// Transactions exceeding this duration will be automatically rolled back.
        /// This property is used as the default value when DefaultTimeoutProvider is not set.
        /// Default: 1 minute
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets a function that dynamically determines the default transaction timeout.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over DefaultTimeout.
        /// The function is called each time a transaction is initiated.
        /// Example: () => TimeSpan.FromSeconds(_configService.GetValue("transaction-timeout-seconds", 60))
        /// Default: null (uses DefaultTimeout instead)
        /// </summary>
        public Func<TimeSpan> DefaultTimeoutProvider { get; set; }

        /// <summary>
        /// Gets or sets whether to automatically roll back the transaction when ICommandResponse.Successful is false.
        /// When true, unsuccessful command responses will not call scope.Complete(), causing rollback.
        /// When false, only exceptions will cause rollback.
        /// This property is used as the default value when RollbackOnUnsuccessfulResponseProvider is not set.
        /// Default: true
        /// </summary>
        public bool RollbackOnUnsuccessfulResponse { get; set; } = true;

        /// <summary>
        /// Gets or sets a function that dynamically determines whether to rollback on unsuccessful responses.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over RollbackOnUnsuccessfulResponse.
        /// The function is called each time a transaction completes.
        /// Example: () => _featureFlagService.IsEnabled("transaction-rollback-on-unsuccessful")
        /// Default: null (uses RollbackOnUnsuccessfulResponse instead)
        /// </summary>
        public Func<bool> RollbackOnUnsuccessfulResponseProvider { get; set; }

        /// <summary>
        /// Gets or sets whether to log transaction start/complete/rollback events.
        /// When true, transaction lifecycle events are logged at Information level.
        /// This property is used as the default value when EnableLoggingProvider is not set.
        /// Default: true
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets a function that dynamically determines whether to log transaction events.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over EnableLogging.
        /// The function is called each time a transaction event needs to be logged.
        /// Example: () => _featureFlagService.IsEnabled("transaction-logging")
        /// Default: null (uses EnableLogging instead)
        /// </summary>
        public Func<bool> EnableLoggingProvider { get; set; }

        /// <summary>
        /// Gets the effective default transaction scope option.
        /// Uses DefaultTransactionScopeOptionProvider if set, otherwise falls back to DefaultTransactionScopeOption.
        /// This method is called each time a transaction is initiated.
        /// </summary>
        /// <returns>The effective default transaction scope option.</returns>
        public TransactionScopeOption GetEffectiveDefaultTransactionScopeOption()
        {
            return DefaultTransactionScopeOptionProvider?.Invoke() ?? DefaultTransactionScopeOption;
        }

        /// <summary>
        /// Gets the effective default isolation level.
        /// Uses DefaultIsolationLevelProvider if set, otherwise falls back to DefaultIsolationLevel.
        /// This method is called each time a transaction is initiated.
        /// </summary>
        /// <returns>The effective default isolation level.</returns>
        public IsolationLevel GetEffectiveDefaultIsolationLevel()
        {
            return DefaultIsolationLevelProvider?.Invoke() ?? DefaultIsolationLevel;
        }

        /// <summary>
        /// Gets the effective default transaction timeout.
        /// Uses DefaultTimeoutProvider if set, otherwise falls back to DefaultTimeout.
        /// This method is called each time a transaction is initiated.
        /// </summary>
        /// <returns>The effective default transaction timeout.</returns>
        public TimeSpan GetEffectiveDefaultTimeout()
        {
            return DefaultTimeoutProvider?.Invoke() ?? DefaultTimeout;
        }

        /// <summary>
        /// Gets the effective setting for rolling back on unsuccessful responses.
        /// Uses RollbackOnUnsuccessfulResponseProvider if set, otherwise falls back to RollbackOnUnsuccessfulResponse.
        /// This method is called each time a transaction completes.
        /// </summary>
        /// <returns>True if transactions should rollback on unsuccessful responses, false otherwise.</returns>
        public bool GetEffectiveRollbackOnUnsuccessfulResponse()
        {
            return RollbackOnUnsuccessfulResponseProvider?.Invoke() ?? RollbackOnUnsuccessfulResponse;
        }

        /// <summary>
        /// Gets the effective setting for logging transaction events.
        /// Uses EnableLoggingProvider if set, otherwise falls back to EnableLogging.
        /// This method is called each time a transaction event needs to be logged.
        /// </summary>
        /// <returns>True if transaction events should be logged, false otherwise.</returns>
        public bool GetEffectiveEnableLogging()
        {
            return EnableLoggingProvider?.Invoke() ?? EnableLogging;
        }
    }
}

