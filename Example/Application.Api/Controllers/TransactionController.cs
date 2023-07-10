using System.Threading.Tasks;
using Data.Entity;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Minded.Extensions.WebApi;
using Service.Transaction.Command;
using Service.Transaction.Query;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    public class TransactionController : Controller
    {
        private readonly IRestMediator _mediator;

        public TransactionController(IRestMediator mediator)
        {
            _mediator = mediator;
        }

        public Task<IActionResult> Get(ODataQueryOptions<Transaction> queryOptions)
        {
            var query = new GetTransactionsQuery(queryOptions);
            return _mediator.ProcessRestQueryAsync(RestOperation.GetMany, query);
        }

        [HttpGet("{id}", Name = "GetTransactionById")]
        public async Task<IActionResult> Get(int id)
        {
            return await _mediator.ProcessRestQueryAsync(RestOperation.GetSingle, new GetTransactionByIdQuery(id));
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] Transaction transaction)
        {
            return await _mediator.ProcessRestCommandAsync<int>(RestOperation.CreateWithContent, new CreateTransactionCommand(transaction));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(int id, [FromBody] Transaction transaction)
        {
            return await _mediator.ProcessRestCommandAsync<int>(RestOperation.UpdateWithContent, new UpdateTransactionCommand(id, transaction));
        }
    }
}
