using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Configuration;
using Minded.Extensions.Logging.Configuration;
using Minded.Framework.CQRS.Abstractions;

namespace Minded.Extensions.Logging.Decorator
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the Logging decorator for Commands using configuration from appsettings.json
        /// </summary>
        /// <param name="builder">MindedBuilder instance</param>
        /// <returns>MindedBuilder for fluent chaining</returns>
        public static MindedBuilder AddCommandLoggingDecorator(this MindedBuilder builder)
        {
            builder.QueueCommandDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(LoggingCommandHandlerDecorator<>)));
            builder.QueueCommandWithResultDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(LoggingCommandHandlerDecorator<,>)));

            builder.ServiceCollection.Configure<LoggingOptions>(builder.Configuration.GetSection("Minded:LoggingOptions"));
            return builder;
        }

        /// <summary>
        /// Add the Logging decorator for Commands with custom configuration action.
        /// Allows programmatic configuration including dynamic providers for all settings (e.g., feature flags).
        /// All LoggingOptions properties support both static values and dynamic providers.
        /// </summary>
        /// <param name="builder">MindedBuilder instance</param>
        /// <param name="configureOptions">Action to configure LoggingOptions</param>
        /// <returns>MindedBuilder for fluent chaining</returns>
        /// <example>
        /// <code>
        /// // Example 1: Static configuration
        /// builder.AddCommandLoggingDecorator(options => {
        ///     options.Enabled = true;
        ///     options.LogOutcomeEntries = true;
        ///     options.MinimumOutcomeSeverityLevel = Severity.Warning;
        /// });
        ///
        /// // Example 2: Dynamic configuration with feature flags
        /// builder.AddCommandLoggingDecorator(options => {
        ///     // All properties can use providers for runtime configuration
        ///     options.EnabledProvider = () => _featureFlagService.IsEnabled("logging-enabled");
        ///     options.LogOutcomeEntriesProvider = () => _featureFlagService.IsEnabled("logging-outcome-entries");
        ///     options.LogMessageTemplateDataProvider = () => _featureFlagService.IsEnabled("logging-template-data");
        ///     options.MinimumOutcomeSeverityLevelProvider = () => _featureFlagService.GetSeverityLevel("logging-min-severity");
        /// });
        ///
        /// // Example 3: Mixed static and dynamic configuration
        /// builder.AddCommandLoggingDecorator(options => {
        ///     options.Enabled = true; // Static - always enabled
        ///     options.LogOutcomeEntries = true; // Static
        ///     // Dynamic severity level from feature flag
        ///     options.MinimumOutcomeSeverityLevelProvider = () => _featureFlagService.GetSeverityLevel("logging-min-severity");
        /// });
        /// </code>
        /// </example>
        public static MindedBuilder AddCommandLoggingDecorator(this MindedBuilder builder, Action<LoggingOptions> configureOptions)
        {
            builder.QueueCommandDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(LoggingCommandHandlerDecorator<>)));
            builder.QueueCommandWithResultDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(LoggingCommandHandlerDecorator<,>)));

            builder.ServiceCollection.Configure(configureOptions);
            return builder;
        }

        /// <summary>
        /// Add the Logging decorator for Queries using configuration from appsettings.json
        /// </summary>
        /// <param name="builder">MindedBuilder instance</param>
        /// <returns>MindedBuilder for fluent chaining</returns>
        public static MindedBuilder AddQueryLoggingDecorator(this MindedBuilder builder)
        {
            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(LoggingQueryHandlerDecorator<,>)));

            builder.ServiceCollection.Configure<LoggingOptions>(builder.Configuration.GetSection("Minded:LoggingOptions"));
            return builder;
        }

        /// <summary>
        /// Add the Logging decorator for Queries with custom configuration action.
        /// Allows programmatic configuration including dynamic providers for all settings (e.g., feature flags).
        /// All LoggingOptions properties support both static values and dynamic providers.
        /// </summary>
        /// <param name="builder">MindedBuilder instance</param>
        /// <param name="configureOptions">Action to configure LoggingOptions</param>
        /// <returns>MindedBuilder for fluent chaining</returns>
        /// <example>
        /// <code>
        /// // Example 1: Static configuration
        /// builder.AddQueryLoggingDecorator(options => {
        ///     options.Enabled = true;
        ///     options.LogOutcomeEntries = true;
        ///     options.MinimumOutcomeSeverityLevel = Severity.Warning;
        /// });
        ///
        /// // Example 2: Dynamic configuration with feature flags
        /// builder.AddQueryLoggingDecorator(options => {
        ///     // All properties can use providers for runtime configuration
        ///     options.EnabledProvider = () => _featureFlagService.IsEnabled("logging-enabled");
        ///     options.LogOutcomeEntriesProvider = () => _featureFlagService.IsEnabled("logging-outcome-entries");
        ///     options.LogMessageTemplateDataProvider = () => _featureFlagService.IsEnabled("logging-template-data");
        ///     options.MinimumOutcomeSeverityLevelProvider = () => _featureFlagService.GetSeverityLevel("logging-min-severity");
        /// });
        ///
        /// // Example 3: Mixed static and dynamic configuration
        /// builder.AddQueryLoggingDecorator(options => {
        ///     options.Enabled = true; // Static - always enabled
        ///     options.LogOutcomeEntries = true; // Static
        ///     // Dynamic severity level from feature flag
        ///     options.MinimumOutcomeSeverityLevelProvider = () => _featureFlagService.GetSeverityLevel("logging-min-severity");
        /// });
        /// </code>
        /// </example>
        public static MindedBuilder AddQueryLoggingDecorator(this MindedBuilder builder, Action<LoggingOptions> configureOptions)
        {
            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(LoggingQueryHandlerDecorator<,>)));

            builder.ServiceCollection.Configure(configureOptions);
            return builder;
        }
    }
}
