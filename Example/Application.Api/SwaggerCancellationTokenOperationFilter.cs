using System.Linq;
using System.Threading;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Application.Api
{
    /// <summary>
    /// Swagger operation filter that removes CancellationToken parameters from the API documentation.
    /// CancellationToken is automatically provided by ASP.NET Core and should not be exposed in the API.
    /// </summary>
    public class SwaggerCancellationTokenOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies the filter to remove CancellationToken parameters from Swagger documentation.
        /// </summary>
        /// <param name="operation">The Swagger operation to modify</param>
        /// <param name="context">The operation filter context containing parameter information</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                return;

            // Find and remove all CancellationToken parameters
            var cancellationTokenParameters = operation.Parameters
                .Where(p => p.Name == "cancellationToken" || 
                           (context.ApiDescription.ParameterDescriptions
                               .Any(pd => pd.Name == p.Name && 
                                         pd.Type == typeof(CancellationToken))))
                .ToList();

            foreach (var parameter in cancellationTokenParameters)
            {
                operation.Parameters.Remove(parameter);
            }
        }
    }
}

