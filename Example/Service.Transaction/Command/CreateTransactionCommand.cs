using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace Service.Transaction.Command
{
    [ValidateCommand]
    public class CreateTransactionCommand : ICommand<Data.Entity.Transaction>, ILoggable
    {
        public Data.Entity.Transaction Transaction { get; set; }

        public CreateTransactionCommand(Data.Entity.Transaction transaction, Guid? traceId = null)
        {
            Transaction = transaction;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "Credit: {Credit} Debit: {Debit} CategoryId: {CategoryId}";

        public string[] LoggingProperties => new[] { "Transaction.Credit", "Transaction.Debit", "Transaction.CategoryId" };
    }
}
