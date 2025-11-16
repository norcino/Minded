using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Application.Api.OData
{
    /// <summary>
    /// Action filter that captures OData $expand parameters and stores them in HttpContext.Items
    /// for use by <see cref="IgnoreNavigationPropertiesResolver"/> during JSON serialization.
    /// </summary>
    /// <remarks>
    /// This filter works in conjunction with <see cref="IgnoreNavigationPropertiesResolver"/> to control
    /// which navigation properties are serialized in JSON responses.
    ///
    /// When an OData request includes a $expand parameter (e.g., $expand=Transactions,User), this filter:
    /// 1. Extracts the property names from the $expand parameter
    /// 2. Stores them in HttpContext.Items using the key <see cref="ODataConstants.ExpandedPropertiesKey"/>
    /// 3. The resolver then checks this list during JSON serialization
    ///
    /// This ensures that only explicitly expanded navigation properties are serialized, preventing:
    /// - Circular reference errors
    /// - Performance issues from loading unwanted data
    /// - Exposing more data than intended
    /// </remarks>
    public class ODataExpandActionFilter : IActionFilter
    {
        /// <summary>
        /// Executes before the action method is invoked.
        /// Captures OData expand parameters from the request and stores them in HttpContext.Items.
        /// </summary>
        /// <param name="context">Action executing context</param>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Find ODataQueryOptions parameter in the action arguments
            var odataQueryOptions = context.ActionArguments.Values
                .FirstOrDefault(v => v != null && v.GetType().IsGenericType && 
                                v.GetType().GetGenericTypeDefinition() == typeof(ODataQueryOptions<>));

            if (odataQueryOptions == null)
            {
                // No OData query options, so no navigation properties should be serialized
                context.HttpContext.Items[ODataConstants.ExpandedPropertiesKey] = new HashSet<string>();
                return;
            }

            // Get the SelectExpand property using reflection
            var selectExpandProperty = odataQueryOptions.GetType().GetProperty("SelectExpand");
            var selectExpand = selectExpandProperty?.GetValue(odataQueryOptions);

            if (selectExpand == null)
            {
                // No $expand parameter, so no navigation properties should be serialized
                context.HttpContext.Items[ODataConstants.ExpandedPropertiesKey] = new HashSet<string>();
                return;
            }

            // Get the RawExpand property (contains the raw $expand string like "Transactions,User")
            var rawExpandProperty = selectExpand.GetType().GetProperty("RawExpand");
            var rawExpand = rawExpandProperty?.GetValue(selectExpand) as string;

            if (string.IsNullOrWhiteSpace(rawExpand))
            {
                // No $expand parameter, so no navigation properties should be serialized
                context.HttpContext.Items[ODataConstants.ExpandedPropertiesKey] = new HashSet<string>();
                return;
            }

            // Parse the expand string and store the property names
            // Format can be: "Transactions" or "Transactions,User" or "Transactions($expand=Category)"
            var expandedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var expand in rawExpand.Split(','))
            {
                // Extract the property name (before any parentheses for nested expands)
                var propertyName = expand.Trim().Split('(')[0].Trim();

                // Handle nested paths like "Transactions/Category" -> just take the first part
                propertyName = propertyName.Split('/')[0].Trim();

                if (!string.IsNullOrWhiteSpace(propertyName))
                {
                    expandedProperties.Add(propertyName);
                }
            }

            context.HttpContext.Items[ODataConstants.ExpandedPropertiesKey] = expandedProperties;
        }

        /// <summary>
        /// Executes after the action method is invoked.
        /// </summary>
        /// <param name="context">Action executed context</param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Nothing to do after action execution
        }
    }
}

