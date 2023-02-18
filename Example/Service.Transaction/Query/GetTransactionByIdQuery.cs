using Minded.Framework.CQRS.Query;

namespace Service.Transaction.Query
{
    public class GetTransactionByIdQuery : IQuery<Data.Entity.Transaction>
    {
        public int TransactionId { get; }

        public GetTransactionByIdQuery(int transactionId)
        {
            TransactionId = transactionId;
        }
    }
}
