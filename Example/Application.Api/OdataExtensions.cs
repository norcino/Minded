using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Api
{
    public static class OdataExtensions {

        public static void X<TEntity>(ODataQueryOptions<TEntity> odataOptions)
        {
            var apply = odataOptions.Apply;
            var context = odataOptions.Context;
            var rawValues = odataOptions.RawValues;
            var count = odataOptions.Count;
            var filter = odataOptions.Filter;
            var orderBy = odataOptions.OrderBy;
            var httpRequest = odataOptions.Request;
            var selectExpand = odataOptions.SelectExpand;
            var skip = odataOptions.Skip;
            var skipToken = odataOptions.SkipToken;
            var top = odataOptions.Top;
            var validator = odataOptions.Validator;
        }

        /// <summary>
        /// Extract the expression used to filter teh queryable. This expression can be applied to the Context.
        /// </summary>
        /// <typeparam name="TEntity">Entity type which will be filtered</typeparam>
        /// <param name="filter">FilterQueryOption filter from the ODataQueryOption</param>
        /// <returns>Expression which can be uset to filter an IQueryable of TEntity</returns>
        public static Expression<Func<TEntity, bool>> GetFilterExpression<TEntity>(this FilterQueryOption filter)
        {
            var enumerable = Enumerable.Empty<TEntity>().AsQueryable();
            var param = Expression.Parameter(typeof(TEntity));
            if (filter != null)
            {
                enumerable = (IQueryable<TEntity>)filter.ApplyTo(enumerable, new ODataQuerySettings());

                var mce = enumerable.Expression as MethodCallExpression;
                if (mce != null)
                {
                    var quote = mce.Arguments[1] as UnaryExpression;
                    if (quote != null)
                    {
                        return quote.Operand as Expression<Func<TEntity, bool>>;
                    }
                }
            }
            return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(true), param);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IEnumerable<T> ApplyODataQueryOptions<T>(this IQueryable<T> query, ODataQueryOptions options) where T : class, new()
        {
            if (options == null)
            {
                return query;
            }

            var queryable = options.ApplyTo(query);

            if (queryable is IQueryable<T> queriableEntity)
            {
                return queriableEntity.AsEnumerable();
            }

            return UnwrapAll<T>(queryable).ToList();
        }

        private static IEnumerable<T> UnwrapAll<T>(this IQueryable queryable) where T : class, new()
        {
            foreach (var item in queryable)
            {
                yield return Unwrap<T>(item);
            }
        }

        private static T Unwrap<T>(object item) where T : class, new()
        {
            var instanceProp = item.GetType().GetProperty("Instance");
            var value = (T)instanceProp.GetValue(item);

            if (value != null)
            {
                return value;
            }

            value = new T();
            var containerProp = item.GetType().GetProperty("Container");
            var container = containerProp.GetValue(item);

            if (container == null)
            {
                return (T)null;
            }

            var containerType = container.GetType();
            var containerItem = container;
            var allNull = true;

            for (var i = 0; containerItem != null; i++)
            {
                var containerItemType = containerItem.GetType();
                var containerItemValue = containerItemType.GetProperty("Value").GetValue(containerItem);

                if (containerItemValue == null)
                {
                    containerItem = containerType.GetProperty($"Next{i}")?.GetValue(container);
                    continue;
                }

                var containerItemName = containerItemType.GetProperty("Name").GetValue(containerItem) as string;
                var expandedProp = typeof(T).GetProperty(containerItemName);

                if (expandedProp.SetMethod == null)
                {
                    containerItem = containerType.GetProperty($"Next{i}")?.GetValue(container);
                    continue;
                }

                if (containerItemValue.GetType() != typeof(string) && containerItemValue is IEnumerable containerValues)
                {
                    var listType = typeof(List<>).MakeGenericType(expandedProp.PropertyType.GenericTypeArguments[0]);
                    var expandedList = (IList)Activator.CreateInstance(listType);

                    foreach (var expandedItem in containerValues)
                    {
                        var expandedInstanceProp = expandedItem.GetType().GetProperty("Instance");
                        var expandedValue = expandedInstanceProp.GetValue(expandedItem);
                        expandedList.Add(expandedValue);
                    }

                    expandedProp.SetValue(value, expandedList);
                    allNull = false;
                }
                else
                {
                    var expandedInstanceProp = containerItemValue.GetType().GetProperty("Instance");

                    if (expandedInstanceProp == null)
                    {
                        expandedProp.SetValue(value, containerItemValue);
                        allNull = false;
                    }
                    else
                    {
                        var expandedValue = expandedInstanceProp.GetValue(containerItemValue);
                        if (expandedValue != null)
                        {
                            expandedProp.SetValue(value, expandedValue);
                            allNull = false;
                        }
                        else
                        {
                            var t = containerItemValue.GetType().GenericTypeArguments[0];
                            var wrapInfo = typeof(OdataExtensions).GetMethod(nameof(Unwrap));
                            var wrapT = wrapInfo.MakeGenericMethod(t);
                            expandedValue = wrapT.Invoke(null, new[] { containerItemValue });
                            if (expandedValue != null)
                            {
                                expandedProp.SetValue(value, expandedValue);
                                allNull = false;
                            }
                        }
                    }
                }
                containerItem = containerType.GetProperty($"Next{i}")?.GetValue(container);
            }

            if (allNull)
            {
                return (T)null;
            }

            return value;
        }
    }
}
