using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minded.Extensions.WebApi;
using MindedExample.Application.Configuration.Command;
using MindedExample.Application.Configuration.Query;
using MindedExample.Api.Models;

namespace MindedExample.Api.Controllers
{
    /// <summary>
    /// Global administration endpoints for tenant lifecycle management.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "GlobalAdminOnly")]
    [Route("api/tenants")]
    [Route("api/admin/tenants")]
    public class TenantsController : ControllerBase
    {
        private readonly IRestMediator _restMediator;

        public TenantsController(IRestMediator restMediator)
        {
            _restMediator = restMediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetTenants(CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestQueryAsync(
                RestOperation.GetMany,
                new GetAdminTenantSummariesQuery(),
                cancellationToken);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request, CancellationToken cancellationToken = default)
        {
            var command = new CreateTenantCommand(
                request?.Name,
                request?.LegalOwnerName,
                request?.LegalOwnerSurname,
                request?.LegalOwnerEmail,
                request?.LegalOwnerPassword);

            return await _restMediator.ProcessRestCommandAsync(
                RestOperation.CreateWithContent,
                command,
                cancellationToken);
        }

        [HttpDelete("{tenantId:int}")]
        public async Task<IActionResult> DeleteTenant(int tenantId, [FromBody] DeleteTenantRequest request, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(
                RestOperation.Delete,
                new DeleteTenantCommand(tenantId, request?.ConfirmationName),
                cancellationToken);
        }
    }
}
