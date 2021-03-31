using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockQueryable.Moq;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace Common.Tests
{
    public static class TestingExtensions
    {
        public static DbSet<T> MockDbSet<T>(this IEnumerable<T> data) where T : class, new()
        {
            return data.AsQueryable<T>().BuildMockDbSet().Object;
        }

        public static Mock<DbSet<T>> GetMockDbSet<T>(this IEnumerable<T> data) where T : class, new()
        {
            return data.AsQueryable<T>().BuildMockDbSet();
        }

        public static DbSet<T> MockDbSet<T>(this IQueryable<T> data) where T : class, new()
        {
            var mock = data.BuildMockDbSet();
            return mock.Object;
        }

        #region IServiceCollection
        public static IServiceCollection OverrideAddScoped<T>(this IServiceCollection serviceCollection, T mockOverride) where T : class
        {
            var serviceDescriptor = serviceCollection.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(T));
            serviceCollection.Remove(serviceDescriptor);
            serviceCollection.AddScoped<T>(s => mockOverride);
            return serviceCollection;
        }

        public static IServiceCollection OverrideAddSingleton<T>(this IServiceCollection serviceCollection, T mockOverride) where T : class
        {
            var serviceDescriptor = serviceCollection.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(T));
            serviceCollection.Remove(serviceDescriptor);
            serviceCollection.AddSingleton<T>(s => mockOverride);
            return serviceCollection;
        }

        public static IServiceCollection OverrideAddTransient<T>(this IServiceCollection serviceCollection, T mockOverride) where T : class
        {
            var serviceDescriptor = serviceCollection.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(T));
            serviceCollection.Remove(serviceDescriptor);
            serviceCollection.AddTransient<T>(s => mockOverride);
            return serviceCollection;
        }
        #endregion
    }
}
