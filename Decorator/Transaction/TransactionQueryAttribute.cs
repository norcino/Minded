using System;
using System.Transactions;

namespace Minded.Decorator.Transaction
{
    public class TransactionQueryAttribute : Attribute
    {
        public TransactionScopeOption TransactionScopeOption { get; set; }
        public IsolationLevel IsolationLevel { get; set; }

        public TransactionQueryAttribute()
        {
            TransactionScopeOption = TransactionScopeOption.Required;
            IsolationLevel = IsolationLevel.ReadCommitted;
        }
    }
}