using Minded.CommandQuery.Query;

namespace Minded.Exception
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