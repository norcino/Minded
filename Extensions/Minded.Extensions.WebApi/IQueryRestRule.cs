using System;
using System.Net;

namespace Minded.Extensions.WebApi
{
    public interface IQueryRestRule
    {
        RestOperation Operation { get; }
        HttpStatusCode ResultStatusCode { get; }
        ContentResponse ContentResponse { get; }
        Func<object, bool> RuleCondition { get; }
    }
}
