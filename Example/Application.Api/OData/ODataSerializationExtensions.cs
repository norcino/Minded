using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Application.Api.OData
{
    /// <summary>
    /// Extension methods for configuring OData navigation property serialization.
    /// </summary>
    public static class ODataSerializationExtensions
    {
        /// <summary>
        /// Configures JSON serialization to only serialize navigation properties when explicitly
        /// requested via OData $expand parameter.
        /// </summary>
        /// <param name="builder">The MVC builder</param>
        /// <param name="isNavigationProperty">Optional custom function to determine if a type is a navigation property.
        /// If not provided, uses default logic that checks for types in the Data.Entity namespace.</param>
        /// <returns>The MVC builder for chaining</returns>
        /// <remarks>
        /// This extension method configures two components:
        /// 1. <see cref="ODataExpandActionFilter"/> - Captures $expand parameters from requests
        /// 2. <see cref="IgnoreNavigationPropertiesResolver"/> - Controls JSON serialization based on $expand
        /// 
        /// Together, these components ensure that navigation properties are only serialized when
        /// explicitly requested via the OData $expand parameter, preventing:
        /// - Circular reference errors
        /// - Performance issues from loading unwanted data
        /// - Exposing more data than intended
        /// 
        /// Example usage in Startup.cs:
        /// <code>
        /// services.AddMvc(options => options.EnableEndpointRouting = false)
        ///     .AddODataNavigationPropertySerialization();
        /// </code>
        /// 
        /// Custom navigation property detection:
        /// <code>
        /// services.AddMvc(options => options.EnableEndpointRouting = false)
        ///     .AddODataNavigationPropertySerialization(type => 
        ///         type.Namespace?.StartsWith("MyApp.Domain") == true);
        /// </code>
        /// </remarks>
        public static IMvcBuilder AddODataNavigationPropertySerialization(
            this IMvcBuilder builder,
            Func<Type, bool> isNavigationProperty = null)
        {
            // Register HttpContextAccessor for accessing HttpContext in services
            builder.Services.AddHttpContextAccessor();

            // Configure JSON serialization with custom contract resolver
            builder.AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                // Build a temporary service provider to get the HttpContextAccessor
                // This is necessary because we're configuring serialization during service registration
                ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
                IHttpContextAccessor httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

                options.SerializerSettings.ContractResolver = new IgnoreNavigationPropertiesResolver(
                    httpContextAccessor,
                    isNavigationProperty);
            });

            // Register the OData expand action filter
            builder.Services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add<ODataExpandActionFilter>();
            });

            return builder;
        }
    }
}

