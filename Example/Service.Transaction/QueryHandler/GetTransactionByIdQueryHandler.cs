using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.CommandQuery.Query;
using Service.Transaction.Query;

namespace Service.Transaction.QueryHandler
{
    public class GetTransactionByIdQueryHandler : IQueryHandler<GetTransactionByIdQuery, Data.Entity.Transaction>
    {
        private readonly IMindedExampleContext _context;

        public GetTransactionByIdQueryHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public Task<Data.Entity.Transaction> HandleAsync(GetTransactionByIdQuery query)
        {
            return _context.Transactions.SingleOrDefaultAsync(c => c.Id == query.TransactionId);
        }
    }
}
