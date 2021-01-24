using Minded.Common;
using Minded.Log;

namespace Service.Transaction.Command
{
    public class UpdateTransactionCommand : ICommand
    {
        public int TransactionId { get; set; }
        public Data.Entity.Transaction Transaction { get; set; }

        public UpdateTransactionCommand(int id, Data.Entity.Transaction transaction)
        {
            TransactionId = id;
            Transaction = transaction;
        }

        public LogInfo ToLog()
        {
            const string template = "Transaction Id: {TransactionId} Credit: {Credit} Debit: {Debit} CategoryId: {CategoryId}";
            return new LogInfo(template, TransactionId, Transaction.Credit, Transaction.Debit, Transaction.CategoryId);
        }
    }
}
