using System.Threading;
using System.Threading.Tasks;
using Common.Configuration;
using Data.Entity;
using Minded.Framework.CQRS.Query;
using Service.Configuration.Query;

namespace Service.Configuration.QueryHandler
{
    /// <summary>
    /// Handler for retrieving a specific configuration entry by key.
    /// Combines metadata with current value from the configuration store.
    /// </summary>
    public class GetConfigurationByKeyQueryHandler : IQueryHandler<GetConfigurationByKeyQuery, ConfigurationEntry>
    {
        private readonly RuntimeConfigurationStore _configStore;
        private readonly ConfigurationMetadataProvider _metadataProvider;

        public GetConfigurationByKeyQueryHandler(
            RuntimeConfigurationStore configStore,
            ConfigurationMetadataProvider metadataProvider)
        {
            _configStore = configStore;
            _metadataProvider = metadataProvider;
        }

        public async Task<ConfigurationEntry> HandleAsync(
            GetConfigurationByKeyQuery query, 
            CancellationToken cancellationToken = default)
        {
            var entry = _metadataProvider.GetMetadataByKey(query.Key);
            
            if (entry == null)
            {
                return null; // Validator will handle this
            }

            var configurations = _configStore.GetAllConfigurations();
            if (configurations.TryGetValue(entry.Key, out var value))
            {
                entry.Value = value;
            }

            return await Task.FromResult(entry);
        }
    }
}

