using System.Threading;
using System.Threading.Tasks;
using Data.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Minded.Extensions.WebApi;
using Service.Transaction.Command;
using Service.Transaction.Query;

namespace Application.Api.Controllers
{
    /// <summary>
    /// Controller for managing transactions.
    /// Demonstrates RestMediator usage with CQRS pattern, OData support, and proper CancellationToken handling.
    /// </summary>
    [Route("api/[controller]")]
    public class TransactionController : Controller
    {
        private readonly IRestMediator _restMediator;

        public TransactionController(IRestMediator restMediator)
        {
            _restMediator = restMediator;
        }

        /// <summary>
        /// Gets all transactions with optional OData query parameters.
        /// Supports $filter, $orderby, $top, $skip, $count, and $expand.
        /// </summary>
        /// <param name="queryOptions">OData query options</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>List of transactions matching the query</returns>
        [HttpGet]
        public async Task<IActionResult> Get(ODataQueryOptions<Transaction> queryOptions, CancellationToken cancellationToken = default)
        {
            var query = new GetTransactionsQuery(queryOptions);
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query, cancellationToken);
        }

        /// <summary>
        /// Gets a single transaction by ID.
        /// </summary>
        /// <param name="id">Transaction ID</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>Transaction if found, 404 if not found</returns>
        [HttpGet("{id}", Name = "GetTransactionById")]
        public async Task<IActionResult> Get(int id, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestQueryAsync(RestOperation.GetSingle, new GetTransactionByIdQuery(id), cancellationToken);
        }

        /// <summary>
        /// Creates a new transaction.
        /// </summary>
        /// <param name="transaction">Transaction to create</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>Created transaction with 201 status code</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Transaction transaction, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, new CreateTransactionCommand(transaction), cancellationToken);
        }

        /// <summary>
        /// Updates an existing transaction.
        /// </summary>
        /// <param name="id">Transaction ID to update</param>
        /// <param name="transaction">Updated transaction data</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>Updated transaction with 200 status code, or 404 if not found</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Transaction transaction, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.UpdateWithContent, new UpdateTransactionCommand(id, transaction), cancellationToken);
        }

        /// <summary>
        /// Deletes a transaction by ID.
        /// </summary>
        /// <param name="id">Transaction ID to delete</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>204 No Content if successful, 404 if not found</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(RestOperation.Delete, new DeleteTransactionCommand(id), cancellationToken);
        }
    }
}
