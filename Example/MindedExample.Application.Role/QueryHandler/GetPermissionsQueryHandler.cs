using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Role.Query;
using MindedExample.Domain;

namespace MindedExample.Application.Role.QueryHandler
{
    public class GetPermissionsQueryHandler : IQueryHandler<GetPermissionsQuery, IQueryResponse<IReadOnlyDictionary<string, string[]>>>
    {
        public Task<IQueryResponse<IReadOnlyDictionary<string, string[]>>> HandleAsync(GetPermissionsQuery query, CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<string, string[]> result = Permissions.Groups;
            return Task.FromResult<IQueryResponse<IReadOnlyDictionary<string, string[]>>>(
                new QueryResponse<IReadOnlyDictionary<string, string[]>>(result));
        }
    }
}
