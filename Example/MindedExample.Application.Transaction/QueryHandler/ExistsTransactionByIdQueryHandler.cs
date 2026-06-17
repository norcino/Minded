using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Transaction.Query;

namespace MindedExample.Application.Transaction.QueryHandler
{
 /// <summary>
 /// Handler for <see cref="ExistsTransactionByIdQuery"/>.
 /// Returns true if the Transaction exists, otherwise false.
 /// </summary>
 public class ExistsTransactionByIdQueryHandler : IQueryHandler<ExistsTransactionByIdQuery, bool>
 {
 private readonly IMindedExampleContext _context;
 private readonly ICurrentUserAccessor _currentUserAccessor;

 public ExistsTransactionByIdQueryHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
 {
 _context = context;
 _currentUserAccessor = currentUserAccessor;
 }

 public async Task<bool> HandleAsync(ExistsTransactionByIdQuery query, CancellationToken cancellationToken = default)
 {
 if (!_currentUserAccessor.TenantId.HasValue)
 {
 return false;
 }

 return await _context.Transactions
 .AsNoTracking()
 .AnyAsync(t => t.Id == query.TransactionId && t.User.TenantId == _currentUserAccessor.TenantId.Value, cancellationToken);
 }
 }
}
