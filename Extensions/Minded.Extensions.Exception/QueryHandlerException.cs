using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Exception
{
    public class QueryHandlerException<TQuery, TResult> : System.Exception where TQuery : IQuery<TResult>
    {
        public TQuery Query { get; private set; }

        public QueryHandlerException(TQuery query, string message, System.Exception innerException = null)
            : base(message, innerException)
        {
            Query = query;
        }
    }
}
