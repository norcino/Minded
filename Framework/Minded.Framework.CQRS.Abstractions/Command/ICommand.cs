using System.Transactions;

namespace Minded.Framework.CQRS.Command
{
    public interface ICommand
    {
        
    }

    public interface ICommand<TResult> : ICommand
    {
        TResult Result { get; }
    }

    //public interface ICommandWithTransactionIsolationLevelOverride : ICommand
    //{
    //    IsolationLevel? IsolationLevel { get; set; }
    //}

    //public interface ICommandWithTransactionScopeOptionOverride : ICommand
    //{
    //    TransactionScopeOption? TransactionScopeOption { get; set; }
    //}
}
