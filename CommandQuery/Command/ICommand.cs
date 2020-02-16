using System.Transactions;
using Minded.Log;

namespace Minded.Common
{
    public interface ICommand
    {
        LogInfo ToLog();
    }

    public interface ICommand<TResult> : ICommand
    {
        TResult Result { get; }
    }

    public interface ICommandWithTransactionIsolationLevelOverride : ICommand
    {
        IsolationLevel? IsolationLevel { get; set; }
    }

    public interface ICommandWithTransactionScopeOptionOverride : ICommand
    {
        TransactionScopeOption? TransactionScopeOption { get; set; }
    }
}