using System;
using System.Threading.Tasks;
using Common.Configuration;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Service.Configuration.Command;

namespace Service.Configuration.Validator
{
    /// <summary>
    /// Validator for UpdateConfigurationCommand.
    /// Ensures the configuration key exists and the value is compatible with the expected type.
    /// Returns 404 if key not found, 400 if value is invalid.
    /// </summary>
    public class UpdateConfigurationCommandValidator : ICommandValidator<UpdateConfigurationCommand>
    {
        private readonly ConfigurationMetadataProvider _metadataProvider;

        public UpdateConfigurationCommandValidator(ConfigurationMetadataProvider metadataProvider)
        {
            _metadataProvider = metadataProvider;
        }

        public async Task<IValidationResult> ValidateAsync(UpdateConfigurationCommand command)
        {
            var validationResult = new ValidationResult();

            // Validate key exists
            if (string.IsNullOrWhiteSpace(command.Key))
            {
                validationResult.OutcomeEntries.Add(
                    new OutcomeEntry(
                        nameof(command.Key),
                        "Configuration key cannot be empty",
                        GenericErrorCodes.ValidationFailed,
                        Severity.Error));
                return validationResult;
            }

            var entry = _metadataProvider.GetMetadataByKey(command.Key);
            if (entry == null)
            {
                validationResult.OutcomeEntries.Add(
                    new OutcomeEntry(
                        nameof(command.Key),
                        $"Configuration key '{command.Key}' not found",
                        GenericErrorCodes.SubjectNotFound,
                        Severity.Error));
                return validationResult;
            }

            // Validate value is provided
            if (command.Request?.Value == null)
            {
                validationResult.OutcomeEntries.Add(
                    new OutcomeEntry(
                        "Value",
                        "Configuration value cannot be null",
                        GenericErrorCodes.ValidationFailed,
                        Severity.Error));
                return validationResult;
            }

            // Validate value type compatibility
            try
            {
                ConvertValue(command.Request.Value, entry.Type);
            }
            catch (Exception ex)
            {
                validationResult.OutcomeEntries.Add(
                    new OutcomeEntry(
                        "Value",
                        $"Invalid value for type '{entry.Type}': {ex.Message}",
                        GenericErrorCodes.ValidationFailed,
                        Severity.Error));
            }

            return await Task.FromResult(validationResult);
        }

        private object ConvertValue(object value, string type)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return type.ToLowerInvariant() switch
            {
                "bool" => Convert.ToBoolean(value),
                "int" => Convert.ToInt32(value),
                "string" => value.ToString(),
                _ => throw new NotSupportedException($"Type '{type}' is not supported.")
            };
        }
    }
}

