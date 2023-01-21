//using System.Transactions;
//using Microsoft.Extensions.Logging;

//namespace Minded.Extensions.Transaction.Decorator
//{
//    public class TransactionManager
//    {
//        public static TransactionScope CreateTransactionScope(TransactionScopeOption? scopeOption, IsolationLevel? isolationLevel)
//        {
//            if (scopeOption == null)
//            {
//                scopeOption = TransactionScopeOption.Required;
//            }

//            // Read committed should be used otherwise TransactionScope defaults to SERIALIZABLE which is v.slow
//            // http://msdn.microsoft.com/en-us/library/system.transactions.transactionscope%28v=vs.110%29.aspx
//            var txOptions = new TransactionOptions
//            {
//                IsolationLevel = isolationLevel ?? IsolationLevel.ReadCommitted,
//                Timeout = System.Transactions.TransactionManager.MaximumTimeout
//            };

//            // Inner transaction scope's isolation level cannot be different if sharing the same transaction
//            // In order to change the isolation level we must enforce the creation of a new transaction
//            if (System.Transactions.Transaction.Current != null && System.Transactions.Transaction.Current.IsolationLevel != txOptions.IsolationLevel)
//            {
//                scopeOption = TransactionScopeOption.RequiresNew;
//            }

//            return new TransactionScope((TransactionScopeOption)scopeOption, txOptions, TransactionScopeAsyncFlowOption.Enabled);
//        }

//        public static void LogTransactionStarting(ILogger logger, object transObject)
//        {
//            const string transactionstarting = "Transaction Started: {TranObject:l} TranId: {TranId:l}";

//            var transactionId = string.Empty;
//            if (System.Transactions.Transaction.Current != null)
//            {
//                transactionId = System.Transactions.Transaction.Current.TransactionInformation.LocalIdentifier;
//            }

//            logger.LogInformation(transactionstarting, transactionId, transObject.GetType().Name);
//        }

//        public static void LogTransactionComplete(ILogger logger, object transObject)
//        {
//            const string transactioncomplete = "Transaction Complete: {TranObject:l} {TranId:l}";
//            logger.LogInformation(transactioncomplete, string.Empty, transObject.GetType().Name);
//        }

//        public static TransactionScopeOption? GetTransactionScopeFromObject<T>(object obj)
//        {
//            TransactionScopeOption? scopeOption = null;

//            if (obj is T)
//            {
//                scopeOption = ((dynamic)(T)obj).TransactionScopeOption;
//            }

//            return scopeOption;
//        }

//        public static IsolationLevel? GetIsolationLevelFromObject<T>(object obj)
//        {
//            IsolationLevel? isolationLevel = null;

//            if (obj is T)
//            {
//                isolationLevel = ((dynamic)(T)obj).IsolationLevel;
//            }
//            return isolationLevel;
//        }
//    }
//}
