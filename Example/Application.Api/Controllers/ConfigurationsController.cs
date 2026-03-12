using System.Threading;
using System.Threading.Tasks;
using Data.Entity;
using Microsoft.AspNetCore.Mvc;
using Minded.Extensions.WebApi;
using Service.Configuration.Command;
using Service.Configuration.Query;

namespace Application.Api.Controllers
{
    /// <summary>
    /// Controller for managing runtime configuration of Minded decorators.
    /// Allows viewing and updating configuration options at runtime without application restart.
    /// Uses CQRS pattern with RestMediator for consistent API design.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationsController : ControllerBase
    {
        private readonly IRestMediator _restMediator;

        public ConfigurationsController(IRestMediator restMediator)
        {
            _restMediator = restMediator;
        }

        /// <summary>
        /// Gets all configuration entries with metadata and current values.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>List of all configuration entries</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestQueryAsync(
                RestOperation.GetMany,
                new GetAllConfigurationsQuery(),
                cancellationToken);
        }

        /// <summary>
        /// Gets a specific configuration entry by key.
        /// </summary>
        /// <param name="key">The configuration key (e.g., "Logging.Enabled")</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>Configuration entry if found, 404 if not found</returns>
        [HttpGet("{key}")]
        public async Task<IActionResult> GetByKey(string key, CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestQueryAsync(
                RestOperation.GetSingle,
                new GetConfigurationByKeyQuery(key),
                cancellationToken);
        }

        /// <summary>
        /// Updates a configuration value by key.
        /// </summary>
        /// <param name="key">The configuration key (e.g., "Logging.Enabled")</param>
        /// <param name="request">The update request containing the new value</param>
        /// <param name="cancellationToken">Cancellation token for request cancellation</param>
        /// <returns>Updated configuration entry, 404 if key not found, 400 if value invalid</returns>
        [HttpPut("{key}")]
        public async Task<IActionResult> Update(
            string key,
            [FromBody] UpdateConfigurationRequest request,
            CancellationToken cancellationToken = default)
        {
            return await _restMediator.ProcessRestCommandAsync(
                RestOperation.UpdateWithContent,
                new UpdateConfigurationCommand(key, request),
                cancellationToken);
        }
    }
}
