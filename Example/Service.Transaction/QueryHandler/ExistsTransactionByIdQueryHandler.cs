using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using Service.Transaction.Query;

namespace Service.Transaction.QueryHandler
{
 /// <summary>
 /// Handler for <see cref="ExistsTransactionByIdQuery"/>.
 /// Returns true if the Transaction exists, otherwise false.
 /// </summary>
 public class ExistsTransactionByIdQueryHandler : IQueryHandler<ExistsTransactionByIdQuery, bool>
 {
 private readonly IMindedExampleContext _context;

 public ExistsTransactionByIdQueryHandler(IMindedExampleContext context)
 {
 _context = context;
 }

 public async Task<bool> HandleAsync(ExistsTransactionByIdQuery query, CancellationToken cancellationToken = default)
 {
 return await _context.Transactions
 .AsNoTracking()
 .AnyAsync(t => t.Id == query.TransactionId, cancellationToken);
 }
 }
}
