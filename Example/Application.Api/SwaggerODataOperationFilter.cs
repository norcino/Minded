using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Application.Api
{
    /// <summary>
    /// Swagger operation filter that replaces ODataQueryOptions parameters with standard OData query string parameters.
    /// ODataQueryOptions cannot be represented in JSON, so we expose the individual query string parameters instead.
    /// </summary>
    public class SwaggerODataOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies the filter to replace ODataQueryOptions with individual OData query parameters.
        /// </summary>
        /// <param name="operation">The Swagger operation to modify</param>
        /// <param name="context">The operation filter context containing parameter information</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                return;

            // Find ODataQueryOptions parameters
            var odataParameters = context.ApiDescription.ParameterDescriptions
                .Where(p => p.Type.IsGenericType &&
                           p.Type.GetGenericTypeDefinition() == typeof(ODataQueryOptions<>))
                .ToList();

            if (!odataParameters.Any())
                return;

            // Remove the ODataQueryOptions parameter from Swagger
            foreach (ApiParameterDescription odataParam in odataParameters)
            {
                OpenApiParameter paramToRemove = operation.Parameters
                    .FirstOrDefault(p => p.Name == odataParam.Name);

                if (paramToRemove != null)
                {
                    operation.Parameters.Remove(paramToRemove);
                }
            }

            // Add standard OData query string parameters
            var odataQueryParams = new List<OpenApiParameter>
            {
                new OpenApiParameter
                {
                    Name = "$filter",
                    In = ParameterLocation.Query,
                    Required = false,
                    Description = "Filter the results using OData filter syntax. Example: Name eq 'Electronics'",
                    Schema = new OpenApiSchema { Type = "string" }
                },
                new OpenApiParameter
                {
                    Name = "$orderby",
                    In = ParameterLocation.Query,
                    Required = false,
                    Description = "Order the results. Example: Name desc, Id asc",
                    Schema = new OpenApiSchema { Type = "string" }
                },
                new OpenApiParameter
                {
                    Name = "$top",
                    In = ParameterLocation.Query,
                    Required = false,
                    Description = "Limit the number of results. Example: 10",
                    Schema = new OpenApiSchema { Type = "integer", Format = "int32" }
                },
                new OpenApiParameter
                {
                    Name = "$skip",
                    In = ParameterLocation.Query,
                    Required = false,
                    Description = "Skip a number of results. Example: 20",
                    Schema = new OpenApiSchema { Type = "integer", Format = "int32" }
                },
                new OpenApiParameter
                {
                    Name = "$count",
                    In = ParameterLocation.Query,
                    Required = false,
                    Description = "Include the total count of results. Example: true",
                    Schema = new OpenApiSchema { Type = "boolean" }
                },
                new OpenApiParameter
                {
                    Name = "$expand",
                    In = ParameterLocation.Query,
                    Required = false,
                    Description = "Expand related entities. Example: Category, User",
                    Schema = new OpenApiSchema { Type = "string" }
                },
                new OpenApiParameter
                {
                    Name = "$select",
                    In = ParameterLocation.Query,
                    Required = false,
                    Description = "Select specific properties. Example: Id,Name,Description",
                    Schema = new OpenApiSchema { Type = "string" }
                }
            };

            // Add the OData parameters to the operation
            foreach (OpenApiParameter param in odataQueryParams)
            {
                operation.Parameters.Add(param);
            }
        }
    }
}

