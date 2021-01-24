using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Query;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Data.Common
{
    /// <summary>
    /// Contains Extension for IQueriable objects
    /// </summary>
    public static class QueriableExtensions
    {
        /// <summary>
        /// Transforms an IQueriable in plan SQL
        /// </summary>
        /// <typeparam name="TEntity">Generic entity type for the IQueriable</typeparam>
        /// <param name="query">IQueriable representing the query which would be executed when the IQueriable is materialized</param>
        /// <returns>SQL query generated on the deferred execution</returns>
        public static string ToSql<TEntity>(this IQueryable<TEntity> query) where TEntity : class
        {
            var enumerator = query.Provider.Execute<IEnumerable<TEntity>>(query.Expression).GetEnumerator();
            var relationalCommandCache = enumerator.Private("_relationalCommandCache");
            var selectExpression = relationalCommandCache.Private<SelectExpression>("_selectExpression");
            var factory = relationalCommandCache.Private<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");

            var sqlGenerator = factory.Create();
            var command = sqlGenerator.GetCommand(selectExpression);

            string sql = command.CommandText;
            return sql;
        }

        private static object Private(this object obj, string privateField) => obj?.GetType().GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
        private static T Private<T>(this object obj, string privateField) => (T)obj?.GetType().GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
    }
}
