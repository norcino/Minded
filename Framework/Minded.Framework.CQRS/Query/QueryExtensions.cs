﻿using System.Linq;
//using System.Linq.Dynamic.Core;
//using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query.Trait;

namespace Minded.Framework.CQRS.Query
{
    public static class QueryExtensions
    {
        public static IQueryable<T> ApplyTo<T>(this IQuery<T> query, IQueryable<T> queryable) where T : class
        {
            if (query is ICanOrderBy o && o?.OrderBy?.Count > 0)
            {
                var orderIndex = 0;
                var dynamicOrderString = $"{o.OrderBy[orderIndex].PropertyName} {o.OrderBy[orderIndex].Order}";
                for (orderIndex++; orderIndex < o.OrderBy.Count; orderIndex++)
                {
                    dynamicOrderString += $", {o.OrderBy[orderIndex].PropertyName} {o.OrderBy[orderIndex].Order}";
                }
     //           queryable = queryable.OrderBy(dynamicOrderString);
            }

            if (query is ICanExpand e && e.Expand?.Length > 0)
            {
         //       queryable = e.Expand.Aggregate(queryable, (current, expand) => current.Include(expand));
            }

            //if (query is ICanFilter<T> f && f.Filter != null)
            //{
            //    queryable = queryable.Where(f.Filter);
            //}

            if (query is ICanSkip s)
            {
                queryable = queryable.Skip(s.Skip);
            }

            if (query is ICanTop t && t.Top.HasValue)
            {
                queryable = queryable.Take(t.Top.Value);
            }

            return queryable;
        }
    }
}