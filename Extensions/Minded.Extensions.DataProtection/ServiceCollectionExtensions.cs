using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minded.Extensions.Configuration;
using Minded.Extensions.DataProtection.Abstractions;
using Minded.Framework.CQRS.Abstractions.Sanitization;

namespace Minded.Extensions.DataProtection
{
    /// <summary>
    /// Extension methods for configuring Data Protection services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Data Protection services with default DataSanitizer implementation.
        /// This enables sensitive data protection in logging and exception handling decorators.
        /// </summary>
        /// <param name="builder">The MindedBuilder instance.</param>
        /// <param name="configureOptions">Optional action to configure DataProtectionOptions.</param>
        /// <returns>The MindedBuilder for method chaining.</returns>
        /// <remarks>
        /// This method:
        /// - Registers IDataSanitizer with the default DataSanitizer implementation
        /// - Configures DataProtectionOptions from configuration section "Minded:DataProtectionOptions"
        /// - Allows additional configuration via the configureOptions parameter
        /// 
        /// By default, sensitive data is hidden (ShowSensitiveData = false).
        /// Use configureOptions to customize behavior, such as showing sensitive data in development.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMinded(builder =>
        /// {
        ///     // Add data protection with default settings
        ///     builder.AddDataProtection();
        ///     
        ///     // Or configure options
        ///     builder.AddDataProtection(options =>
        ///     {
        ///         options.ShowSensitiveDataProvider = () => _environment.IsDevelopment();
        ///     });
        /// });
        /// </code>
        /// </example>
        public static MindedBuilder AddDataProtection(this MindedBuilder builder, Action<DataProtectionOptions> configureOptions = null)
        {
            // Register DataProtectionOptions from configuration
            builder.ServiceCollection.Configure<DataProtectionOptions>(
                builder.Configuration.GetSection("Minded:DataProtectionOptions"));

            // Apply additional configuration if provided
            if (configureOptions != null)
            {
                builder.ServiceCollection.Configure(configureOptions);
            }

            // Register the default DataSanitizer implementation
            // Use TryAddSingleton to allow custom implementations to be registered first
            builder.ServiceCollection.TryAddSingleton<IDataSanitizer, DataSanitizer>();

            // Register the DataProtectionLoggingSanitizer as a singleton
            // It will be automatically registered with the pipeline when the pipeline is first resolved
            builder.ServiceCollection.TryAddSingleton<ILoggingSanitizer, DataProtectionLoggingSanitizer>();

            return builder;
        }

        /// <summary>
        /// Adds Data Protection services with a custom IDataSanitizer implementation.
        /// This allows you to provide your own sanitization logic.
        /// </summary>
        /// <typeparam name="TImplementation">The custom IDataSanitizer implementation type.</typeparam>
        /// <param name="builder">The MindedBuilder instance.</param>
        /// <param name="configureOptions">Optional action to configure DataProtectionOptions.</param>
        /// <returns>The MindedBuilder for method chaining.</returns>
        /// <remarks>
        /// Use this method when you need custom sanitization logic beyond the default implementation.
        /// Your custom implementation must implement IDataSanitizer.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMinded(builder =>
        /// {
        ///     // Add data protection with custom sanitizer
        ///     builder.AddDataProtection&lt;MyCustomSanitizer&gt;(options =>
        ///     {
        ///         options.ShowSensitiveData = false;
        ///     });
        /// });
        /// </code>
        /// </example>
        public static MindedBuilder AddDataProtection<TImplementation>(
            this MindedBuilder builder,
            Action<DataProtectionOptions> configureOptions = null)
            where TImplementation : class, IDataSanitizer
        {
            // Register DataProtectionOptions from configuration
            builder.ServiceCollection.Configure<DataProtectionOptions>(
                builder.Configuration.GetSection("Minded:DataProtectionOptions"));

            // Apply additional configuration if provided
            if (configureOptions != null)
            {
                builder.ServiceCollection.Configure(configureOptions);
            }

            // Register the custom DataSanitizer implementation
            builder.ServiceCollection.AddSingleton<IDataSanitizer, TImplementation>();

            // Register the DataProtectionLoggingSanitizer as a singleton
            // It will be automatically registered with the pipeline when the pipeline is first resolved
            builder.ServiceCollection.TryAddSingleton<ILoggingSanitizer, DataProtectionLoggingSanitizer>();

            return builder;
        }
    }
}

