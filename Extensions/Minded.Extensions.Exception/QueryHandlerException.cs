
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Exception
{
    public class QueryHandlerException<TQuery, TResult> : System.Exception where TQuery : IQuery<TResult>
    {
        public TQuery Query { get; private set; }

        public QueryHandlerException(string message, System.Exception innerException, TQuery query)
            : base(message, innerException)
        {
            Query = query;
        }
    }
}
