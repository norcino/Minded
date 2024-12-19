using System.Net;

namespace Minded.Extensions.WebApi
{
    public interface IMessageRestRule
    {
        RestOperation Operation { get; }
        HttpStatusCode ResultStatusCode { get; }
        ContentResponse ContentResponse { get; }
    }
}
