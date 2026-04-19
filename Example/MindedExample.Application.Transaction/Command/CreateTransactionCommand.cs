using System;
using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Extensions.Authorization.Attributes;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.Transaction.Command
{
    [ValidateCommand]
    [RequirePermissions(MindedExample.Domain.Permissions.CanCreateTransaction)]
    public class CreateTransactionCommand : ICommand<MindedExample.Domain.Transaction>, ILoggable
    {
        public MindedExample.Domain.Transaction Transaction { get; set; }

        public CreateTransactionCommand(MindedExample.Domain.Transaction transaction, Guid? traceId = null)
        {
            Transaction = transaction;
            TraceId = traceId ?? TraceId;
        }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "Description: {Description} Credit: {Credit} Debit: {Debit} CategoryId: {CategoryId}";

        public string[] LoggingProperties => ["Transaction.Description", "Transaction.Credit", "Transaction.Debit", "Transaction.CategoryId"];
    }
}