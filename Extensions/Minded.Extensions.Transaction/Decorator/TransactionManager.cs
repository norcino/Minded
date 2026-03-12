using System;
using System.Transactions;
using Microsoft.Extensions.Logging;

namespace Minded.Extensions.Transaction.Decorator
{
    /// <summary>
    /// Helper class for creating and managing transaction scopes.
    /// Provides methods for creating transaction scopes with proper configuration
    /// and logging transaction lifecycle events.
    /// </summary>
    public static class TransactionManager
    {
        /// <summary>
        /// Creates a TransactionScope with the specified options and async flow enabled.
        /// Automatically handles isolation level conflicts by creating a new transaction when necessary.
        /// </summary>
        /// <param name="scopeOption">The transaction scope option (Required, RequiresNew, Suppress)</param>
        /// <param name="isolationLevel">The isolation level for the transaction</param>
        /// <param name="timeout">The transaction timeout. If null, uses TransactionManager.MaximumTimeout</param>
        /// <returns>A new TransactionScope configured for async/await operations</returns>
        public static TransactionScope CreateTransactionScope(
            TransactionScopeOption scopeOption,
            IsolationLevel isolationLevel,
            TimeSpan? timeout = null)
        {
            // Read committed should be used otherwise TransactionScope defaults to SERIALIZABLE which is very slow
            // Reference: http://msdn.microsoft.com/en-us/library/system.transactions.transactionscope%28v=vs.110%29.aspx
            var txOptions = new TransactionOptions
            {
                IsolationLevel = isolationLevel,
                Timeout = timeout ?? System.Transactions.TransactionManager.MaximumTimeout
            };

            // Inner transaction scope's isolation level cannot be different if sharing the same transaction
            // In order to change the isolation level we must enforce the creation of a new transaction
            if (System.Transactions.Transaction.Current != null &&
                System.Transactions.Transaction.Current.IsolationLevel != txOptions.IsolationLevel)
            {
                scopeOption = TransactionScopeOption.RequiresNew;
            }

            // TransactionScopeAsyncFlowOption.Enabled is critical for async/await support
            // Without this, the transaction context will not flow across async boundaries
            return new TransactionScope(scopeOption, txOptions, TransactionScopeAsyncFlowOption.Enabled);
        }

        /// <summary>
        /// Logs the start of a transaction with transaction ID and object type.
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="messageType">The type of message (command/query) being processed</param>
        /// <param name="isolationLevel">The isolation level of the transaction</param>
        public static void LogTransactionStarting(ILogger logger, Type messageType, IsolationLevel isolationLevel)
        {
            var transactionId = GetCurrentTransactionId();
            logger.LogInformation(
                "Transaction started for {MessageType} with IsolationLevel={IsolationLevel}, TransactionId={TransactionId}",
                messageType.Name,
                isolationLevel,
                transactionId);
        }

        /// <summary>
        /// Logs the successful completion of a transaction.
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="messageType">The type of message (command/query) being processed</param>
        public static void LogTransactionComplete(ILogger logger, Type messageType)
        {
            var transactionId = GetCurrentTransactionId();
            logger.LogInformation(
                "Transaction completed successfully for {MessageType}, TransactionId={TransactionId}",
                messageType.Name,
                transactionId);
        }

        /// <summary>
        /// Logs the rollback of a transaction due to unsuccessful response.
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="messageType">The type of message (command/query) being processed</param>
        public static void LogTransactionRolledBackDueToUnsuccessfulResponse(ILogger logger, Type messageType)
        {
            var transactionId = GetCurrentTransactionId();
            logger.LogWarning(
                "Transaction rolled back for {MessageType} due to unsuccessful response, TransactionId={TransactionId}",
                messageType.Name,
                transactionId);
        }

        /// <summary>
        /// Logs the rollback of a transaction due to an exception.
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="messageType">The type of message (command/query) being processed</param>
        /// <param name="exception">The exception that caused the rollback</param>
        public static void LogTransactionRolledBackDueToException(ILogger logger, Type messageType, Exception exception)
        {
            var transactionId = GetCurrentTransactionId();
            logger.LogError(exception,
                "Transaction rolled back for {MessageType} due to exception, TransactionId={TransactionId}",
                messageType.Name,
                transactionId);
        }

        /// <summary>
        /// Gets the current transaction ID if a transaction is active, otherwise returns "N/A".
        /// </summary>
        /// <returns>The transaction ID or "N/A"</returns>
        private static string GetCurrentTransactionId()
        {
            return System.Transactions.Transaction.Current?.TransactionInformation.LocalIdentifier ?? "N/A";
        }
    }
}
