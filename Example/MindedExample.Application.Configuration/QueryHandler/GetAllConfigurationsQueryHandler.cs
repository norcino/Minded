using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Configuration;
using MindedExample.Domain;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.Configuration.Query;

namespace MindedExample.Application.Configuration.QueryHandler
{
    /// <summary>
    /// Handler for retrieving all configuration entries.
    /// Combines metadata from ConfigurationMetadataProvider with current values from RuntimeConfigurationStore.
    /// </summary>
    public class GetAllConfigurationsQueryHandler : IQueryHandler<GetAllConfigurationsQuery, IQueryResponse<IEnumerable<ConfigurationEntry>>>
    {
        private readonly RuntimeConfigurationStore _configStore;
        private readonly ConfigurationMetadataProvider _metadataProvider;

        public GetAllConfigurationsQueryHandler(
            RuntimeConfigurationStore configStore,
            ConfigurationMetadataProvider metadataProvider)
        {
            _configStore = configStore;
            _metadataProvider = metadataProvider;
        }

        public async Task<IQueryResponse<IEnumerable<ConfigurationEntry>>> HandleAsync(
            GetAllConfigurationsQuery query, 
            CancellationToken cancellationToken = default)
        {
            var configurations = _configStore.GetAllConfigurations();
            var entries = _metadataProvider.GetAllMetadata();

            // Update current values from store
            foreach (var entry in entries)
            {
                if (configurations.TryGetValue(entry.Key, out var value))
                {
                    entry.Value = value;
                }
            }

            return await Task.FromResult(new QueryResponse<IEnumerable<ConfigurationEntry>>(entries));
        }
    }
}

