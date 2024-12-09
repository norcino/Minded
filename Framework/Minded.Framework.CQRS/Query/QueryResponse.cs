using System.Collections.Generic;
using Minded.Framework.CQRS.Abstractions;

namespace Minded.Framework.CQRS.Query
{
    /// <summary>
    /// Query response object
    /// </summary>
    /// <typeparam name="TResult">Query result</typeparam>
    public class QueryResponse<TResult> : IQueryResponse<TResult>
    {
        public QueryResponse() => OutcomeEntries = new List<IOutcomeEntry>();

        public QueryResponse(TResult result) : this()
        {
            Result = result;
            Successful = true;
        }

        public TResult Result { get; }

        public bool Successful { get; set; }

        public List<IOutcomeEntry> OutcomeEntries { get; set; }
    }
}
