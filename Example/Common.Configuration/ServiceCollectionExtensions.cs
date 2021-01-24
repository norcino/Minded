using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static List<ServiceDescriptor> GetDescriptors(this IServiceCollection services, Type serviceType)
        {
            var descriptors = services.Where(service => service.ServiceType == serviceType).ToList();

            if (descriptors.Count == 0)
            {
                throw new InvalidOperationException($"Unable to find registered services for the type '{serviceType.FullName}'");
            }

            return descriptors;
        }
    }
}
