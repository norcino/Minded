using System;
using System.Transactions;

namespace Minded.Decorator.Transaction
{
    public class TransactionCommandAttribute : Attribute
    {
        public TransactionScopeOption TransactionScopeOption { get; set; }
        public IsolationLevel IsolationLevel { get; set; }

        public TransactionCommandAttribute()
        {
            TransactionScopeOption = TransactionScopeOption.Required;
            IsolationLevel = IsolationLevel.ReadCommitted;
        }
    }
}