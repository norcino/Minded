using Minded.CommandQuery.Query;
using Minded.Log;

namespace Service.Transaction.Query
{
    public class GetTransactionByIdQuery : IQuery<Data.Entity.Transaction>
    {
        public int TransactionId { get; }

        public GetTransactionByIdQuery(int transactionId)
        {
            TransactionId = transactionId;
        }
        
        public LogInfo ToLog()
        {
            const string template = "TransactionId: {TransactionId}";
            return new LogInfo(template, TransactionId);
        }
    }
}
