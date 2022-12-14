using System;
using System.Transactions;

namespace Minded.Extensions.Transaction.Decorator
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
