using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Entity;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Minded.Mediator;
using Service.Transaction.Command;
using Service.Transaction.Query;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    public class TransactionController : BaseController
    {
        private readonly IMediator _mediator;

        public TransactionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id}", Name = "GetTransactionById")]
        public async Task<ActionResult> GetByIdAsync(int id)
        {
            var result = await _mediator.ProcessQueryAsync(new GetTransactionByIdQuery(id));

            if (result == null)
            {
                return NotFound();
            }

            return new OkObjectResult(result);
        }

        public Task<List<Transaction>> Get(ODataQueryOptions<Transaction> queryOptions)
        {
            var query = ApplyODataQueryConditions<Transaction, GetTransactionsQuery>(queryOptions, new GetTransactionsQuery());
            return _mediator.ProcessQueryAsync(query);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync([FromBody] Transaction transaction)
        {
            var result = await _mediator.ProcessCommandAsync<int>(new CreateTransactionCommand(transaction));

            if (result.Successful)
            {
                return new CreatedAtRouteResult("GetTransactionById", new { Id = result.Result }, result);
            }

            return StatusCode(StatusCodes.Status400BadRequest, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> PutAsync(int id, [FromBody] Transaction transaction)
        {
            var result = await _mediator.ProcessCommandAsync<int>(new UpdateTransactionCommand(id, transaction));
            return new OkObjectResult(result);
        }
    }
}
