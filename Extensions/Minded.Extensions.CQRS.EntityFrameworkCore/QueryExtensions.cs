using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query.Trait;

namespace Minded.Framework.CQRS.Query
{
    /// <summary>
    /// Extension methods that apply <see cref="IQuery{TResult}"/> trait-based configuration
    /// (filtering, ordering, pagination, eager loading) to an <see cref="IQueryable{T}"/> source
    /// via Entity Framework Core.
    /// </summary>
    public static class QueryExtensions
    {
        /// <summary>
        /// Applies the query trait configuration to an <see cref="IQueryable{T}"/> and returns the
        /// shaped queryable for use with <see cref="IQueryResponse{TResult}"/> wrapping an
        /// <see cref="IEnumerable{T}"/>.
        /// Respects <see cref="ICanOrderBy"/>, <see cref="ICanExpand"/>, <see cref="ICanFilterExpression{T}"/>,
        /// <see cref="ICanSkip"/>, <see cref="ICanTop"/> and <see cref="ICanCount"/> traits.
        /// Defaults to a maximum of 100 results when <see cref="ICanTop"/> is not set.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">Query carrying the trait configuration.</param>
        /// <param name="queryable">IQueryable source to shape.</param>
        /// <returns>Shaped <see cref="IQueryable{T}"/> ready for materialisation.</returns>
        public static IQueryable<T> ApplyQueryTo<T>(this IQuery<IQueryResponse<IEnumerable<T>>> query, IQueryable<T> queryable) where T : class
        {
            if (query is ICanOrderBy o && o?.OrderBy?.Count > 0)
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                IOrderedQueryable<T> oq = null;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                foreach (OrderDescriptor order in o.OrderBy)
                {
                    if (order.Order == Order.Ascending)
                    {
                        if (oq == null)
                            oq = queryable.OrderBy(LambdaOf<T>(order.PropertyName));
                        else
                            oq = oq.ThenBy(LambdaOf<T>(order.PropertyName));
                    }
                    else
                    {
                        if (oq == null)
                            oq = queryable.OrderByDescending(LambdaOf<T>(order.PropertyName));
                        else
                            oq = oq.ThenByDescending(LambdaOf<T>(order.PropertyName));
                    }
                }

                queryable = oq ?? queryable;
            }

            if (query is ICanExpand e && e.Expand?.Length > 0)
            {
                queryable = e.Expand.Aggregate(queryable, (current, expand) => current.Include(ResolveIncludePath<T>(expand)));
            }

            if (query is ICanFilterExpression<T> f && f.Filter != null)
            {
                queryable = queryable.Where(f.Filter);
            }

            // Count must reflect all rows matching the query criteria, so it is taken before pagination
            if (query is ICanCount c && c.Count)
            {
                c.CountValue = queryable.Count();
                if (c.CountOnly)
                    return queryable.Take(0);
            }

            if (query is ICanSkip s && s.Skip.HasValue)
            {
                queryable = queryable.Skip(s.Skip.Value);
            }

            if (query is ICanTop t && t.Top.HasValue)
            {
                queryable = queryable.Take(t.Top.Value);
            }
            else
            {
                queryable = queryable.Take(100);
            }

            return queryable;
        }

        /// <summary>
        /// Applies the query trait configuration to an <see cref="IQueryable{T}"/> and returns the
        /// shaped queryable for use with queries returning a plain <see cref="IEnumerable{T}"/>.
        /// Respects <see cref="ICanOrderBy"/>, <see cref="ICanExpand"/>, <see cref="ICanFilterExpression{T}"/>,
        /// <see cref="ICanSkip"/>, <see cref="ICanTop"/> and <see cref="ICanCount"/> traits.
        /// Defaults to a maximum of 100 results when <see cref="ICanTop"/> is not set.
        /// </summary>
        /// <typeparam name="T">Type of the elements in the resulting IEnumerable</typeparam>
        /// <param name="query">Query</param>
        /// <param name="queryable">IQueryable where the fetch the entity from</param>
        /// <returns>List of found entities</returns>
        public static IQueryable<T> ApplyQueryTo<T>(this IQuery<IEnumerable<T>> query, IQueryable<T> queryable) where T : class
        {
            if (query is ICanOrderBy o && o?.OrderBy?.Count > 0)
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                IOrderedQueryable<T> oq = null;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                foreach(OrderDescriptor order in o.OrderBy)
                {
                    if(order.Order == Order.Ascending)
                    {
                        if(oq == null)
                            oq = queryable.OrderBy(LambdaOf<T>(order.PropertyName));
                        else
                            oq = oq.ThenBy(LambdaOf<T>(order.PropertyName));
                    }
                    else
                    {
                        if (oq == null)
                            oq = queryable.OrderByDescending(LambdaOf<T>(order.PropertyName));
                        else
                            oq = oq.ThenByDescending(LambdaOf<T>(order.PropertyName));
                    }
                }

                queryable = oq ?? queryable;
            }

            if (query is ICanExpand e && e.Expand?.Length > 0)
            {
               queryable = e.Expand.Aggregate(queryable, (current, expand) => current.Include(ResolveIncludePath<T>(expand)));
            }

            if (query is ICanFilterExpression<T> f && f.Filter != null)
            {
                queryable = queryable.Where(f.Filter);
            }

            // Count must reflect all rows matching the query criteria, so it is taken before pagination
            if (query is ICanCount c && c.Count)
            {
                c.CountValue = queryable.Count();
                if (c.CountOnly)
                    return queryable.Take(0);
            }

            if (query is ICanSkip s && s.Skip.HasValue)
            {
                queryable = queryable.Skip(s.Skip.Value);
            }

            if (query is ICanTop t && t.Top.HasValue)
            {
                queryable = queryable.Take(t.Top.Value);
            }
            else
            {
                queryable = queryable.Take(100);
            }

            return queryable;
        }

        /// <summary>
        /// Applies filter and expand traits to the queryable and returns the first matching entity,
        /// or <c>null</c> when no match is found.
        /// Note: <see cref="ICanCount"/>, <see cref="ICanTop"/>, <see cref="ICanSkip"/> and <see cref="ICanOrderBy"/> traits
        /// are intentionally ignored because they are not meaningful for single-entity queries.
        /// </summary>
        /// <typeparam name="T">Returned type</typeparam>
        /// <param name="query">Query</param>
        /// <param name="queryable">IQueryable where the fetch the entity from</param>
        /// <returns>Found entity or null</returns>
        public async static Task<T> ApplyQueryTo<T>(this IQuery<T> query, IQueryable<T> queryable) where T : class
        {
            if (query is ICanExpand e && e.Expand?.Length > 0)
            {
                queryable = e.Expand.Aggregate(queryable, (current, expand) => current.Include(ResolveIncludePath<T>(expand)));
            }

            if (query is ICanFilterExpression<T> f && f.Filter != null)
            {
                queryable = queryable.Where(f.Filter);
            }

#pragma warning disable CS8603 // Possible null reference return.
            return await queryable.FirstOrDefaultAsync();
#pragma warning restore CS8603 // Possible null reference return.
        }

        /// <summary>
        /// Applies filter and expand traits to the queryable and returns the first matching entity
        /// wrapped in a successful <see cref="IQueryResponse{TResult}"/>; the response result is <c>null</c> when no match is found.
        /// Note: <see cref="ICanCount"/>, <see cref="ICanTop"/>, <see cref="ICanSkip"/> and <see cref="ICanOrderBy"/> traits
        /// are intentionally ignored because they are not meaningful for single-entity queries.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">Query carrying the trait configuration.</param>
        /// <param name="queryable">IQueryable source to query against.</param>
        /// <returns>An <see cref="IQueryResponse{TResult}"/> wrapping the first matched entity.</returns>
        public async static Task<IQueryResponse<T>> ApplyQueryTo<T>(this IQuery<IQueryResponse<T>> query, IQueryable<T> queryable) where T : class
        {
            if (query is ICanExpand e && e.Expand?.Length > 0)
            {
                queryable = e.Expand.Aggregate(queryable, (current, expand) => current.Include(ResolveIncludePath<T>(expand)));
            }

            if (query is ICanFilterExpression<T> f && f.Filter != null)
            {
                queryable = queryable.Where(f.Filter);
            }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            T result = await queryable.FirstOrDefaultAsync();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return new QueryResponse<T>(result);
        }

        #region Private methods
        /// <summary>
        /// Builds a lambda expression of the form <c>x => (object)x.PropertyName</c>
        /// used to pass a property selector to LINQ ordering methods.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="propertyName">Name of the property to project (case-sensitive).</param>
        /// <returns>A compiled lambda suitable for use with <see cref="Queryable.OrderBy{TSource,TKey}"/>.</returns>
        private static Expression<Func<T, object>> LambdaOf<T>(string propertyName)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T));
            MemberExpression property = Expression.Property(parameter, propertyName);
            UnaryExpression propertyObject = Expression.Convert(property, typeof(object));

            return Expression.Lambda<Func<T, object>>(propertyObject, parameter);
        }

        /// <summary>
        /// Resolves an expand path against the CLR type <typeparamref name="T"/>, matching each
        /// path segment case-insensitively to the actual navigation property name. This guards
        /// against casing mismatches (for example a lowercase OData <c>$expand</c> value) that would
        /// otherwise cause Entity Framework Core's string-based <c>Include</c> to fail.
        /// Segments that cannot be resolved are preserved as-is so the original EF Core error surfaces.
        /// </summary>
        /// <typeparam name="T">Root entity type the expand path starts from.</typeparam>
        /// <param name="expand">Dot-separated navigation property path (case-insensitive).</param>
        /// <returns>The expand path with each resolvable segment normalised to its declared property name.</returns>
        private static string ResolveIncludePath<T>(string expand)
        {
            if (string.IsNullOrWhiteSpace(expand))
                return expand;

            Type currentType = typeof(T);
            string[] segments = expand.Split('.');

            for (int i = 0; i < segments.Length; i++)
            {
                PropertyInfo property = currentType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => string.Equals(p.Name, segments[i], StringComparison.OrdinalIgnoreCase));

                if (property == null)
                    break;

                segments[i] = property.Name;
                currentType = GetNavigationTargetType(property.PropertyType);
            }

            return string.Join(".", segments);
        }

        /// <summary>
        /// Returns the entity type targeted by a navigation property, unwrapping collection navigations
        /// (for example <see cref="ICollection{T}"/>) to their element type.
        /// </summary>
        /// <param name="propertyType">CLR type of the navigation property.</param>
        /// <returns>The target entity type of the navigation.</returns>
        private static Type GetNavigationTargetType(Type propertyType)
        {
            if (propertyType.IsArray)
                return propertyType.GetElementType();

            if (propertyType.IsGenericType)
                return propertyType.GetGenericArguments().FirstOrDefault() ?? propertyType;

            return propertyType;
        }
        #endregion
    }
}
