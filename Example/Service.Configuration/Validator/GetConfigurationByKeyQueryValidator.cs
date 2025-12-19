using System.Threading.Tasks;
using Common.Configuration;
using Data.Entity;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Service.Configuration.Query;

namespace Service.Configuration.Validator
{
    /// <summary>
    /// Validator for GetConfigurationByKeyQuery.
    /// Ensures the configuration key exists in the metadata.
    /// Returns 404 if the key is not found.
    /// </summary>
    public class GetConfigurationByKeyQueryValidator : IQueryValidator<GetConfigurationByKeyQuery, ConfigurationEntry>
    {
        private readonly ConfigurationMetadataProvider _metadataProvider;

        public GetConfigurationByKeyQueryValidator(ConfigurationMetadataProvider metadataProvider)
        {
            _metadataProvider = metadataProvider;
        }

        public async Task<IValidationResult> ValidateAsync(GetConfigurationByKeyQuery query)
        {
            var validationResult = new ValidationResult();

            if (string.IsNullOrWhiteSpace(query.Key))
            {
                validationResult.OutcomeEntries.Add(
                    new OutcomeEntry(
                        nameof(query.Key),
                        "Configuration key cannot be empty",
                        GenericErrorCodes.ValidationFailed,
                        Severity.Error));
                return validationResult;
            }

            var entry = _metadataProvider.GetMetadataByKey(query.Key);
            if (entry == null)
            {
                validationResult.OutcomeEntries.Add(
                    new OutcomeEntry(
                        nameof(query.Key),
                        $"Configuration key '{query.Key}' not found",
                        GenericErrorCodes.SubjectNotFound,
                        Severity.Error));
            }

            return await Task.FromResult(validationResult);
        }
    }
}

