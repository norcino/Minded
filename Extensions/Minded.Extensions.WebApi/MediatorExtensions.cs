using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Minded.Extensions.WebApi
{
    public class CustomActionResult : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context) => throw new NotImplementedException();
    }

    public static class MediatorExtensions
    {
        //public async Task<IActionResult> ProcessGetApiRequestQueryAsync<TResult>(this IMediator mediator, IQuery<TResult> query)
        //{
        //    var result = await mediator.ProcessQueryAsync(query);
        //    return 
        //}
    }
}
