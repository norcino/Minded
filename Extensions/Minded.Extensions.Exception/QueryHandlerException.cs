using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Exception
{
    public class QueryHandlerException<TQuery, TResult> : System.Exception where TQuery : IQuery<TResult>
    {
        public TQuery Query { get; private set; }
        public string ErrorCode { get; set; } = GenericErrorCodes.Unknown;

        public QueryHandlerException(TQuery query, string message, string errorCode, System.Exception innerException = null)
            : base(message, innerException)
        {
            Query = query;
            ErrorCode = errorCode;
        }

        public QueryHandlerException(TQuery query, string message, System.Exception innerException = null)
            : base(message, innerException)
        {
            Query = query;
        }
    }
}
