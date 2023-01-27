using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
//using Minded.Framework.CQRS.Query.Trait;

namespace Application.Api.Controllers
{
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// Apply to the Query object, the query conditions gathered from the ODataQueryOption
        /// NOTE: Check http://odata.github.io/odata.net/ for Odata Parser
        /// </summary>
        /// <typeparam name="TEntity">Type of the result of the query</typeparam>
        /// <typeparam name="TQuery">Type of the query object</typeparam>
        /// <param name="queryOptions">ODataQueryOptions containing the filtering information</param>
        /// <param name="query">Query object to be decorated</param>
        /// <returns>Query decorated with Expansion, Filter, Ordering and Pagination information</returns>
        //public TQuery ApplyODataQueryConditions<TEntity, TQuery>(ODataQueryOptions queryOptions, TQuery query)
        //{            
        //    if (queryOptions == null) return query;

        //    if (query is ICanCount q && queryOptions.Count != null)
        //    {
        //        q.Count = queryOptions.Count?.Value ?? false;
        //    }
            
        //    if (query is ICanExpand e)
        //    {
        //        e.Expand = queryOptions.SelectExpand?.RawExpand?.Split(',');
        //    }
            
        //    if (query is ICanFilter<TEntity> f && queryOptions.Filter != null)
        //    {
        //       // f.Filter = queryOptions.Filter.GetFilterExpression<TEntity>();
        //    }

        //    if (query is ICanOrderBy o && queryOptions.OrderBy != null)
        //    {
        //        o.OrderBy = new List<OrderDescriptor>();
        //        var orderClause = queryOptions.OrderBy.OrderByClause;
        //        while (orderClause != null)
        //        {
        //            var orderDescriptor = new OrderDescriptor
        //            {
        //                Order = orderClause.Direction == OrderByDirection.Ascending ? Order.Ascending : Order.Descending,
        //                PropertyName = ((EdmNamedElement)((SingleValuePropertyAccessNode)orderClause.Expression).Property).Name
        //            };

        //            o.OrderBy.Add(orderDescriptor);
        //            orderClause = orderClause.ThenBy;
        //        }
        //    }

        //    if (query is ICanSkip s && queryOptions.Skip != null)
        //    {
        //        s.Skip = queryOptions.Skip?.Value ?? 0;
        //    }

        //    if (query is ICanTop)
        //    {
        //        var maxNumberOfResults = queryOptions.Context.DefaultQuerySettings.MaxTop ?? 0;
        //        var queryLimit = queryOptions?.Top?.Value ?? 0;
                
        //        ((ICanTop)query).Top = queryLimit > 0 ? Math.Min(queryLimit, maxNumberOfResults) : maxNumberOfResults;
        //    }

        //    return query;
        //}
    }
}
