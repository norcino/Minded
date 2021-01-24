using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockQueryable.Moq;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Common.Tests
{
    public static class TestingExtensions
    {
        #region HttpResponseMessage
        /// <summary>
        /// Get the message response content as the specified type T
        /// </summary>
        /// <typeparam name="T">Type used to deserialize the content of the response</typeparam>
        /// <param name="response">HttpResponseMessage from where the Content is taken</param>
        /// <returns>Content as the type specified</returns>
        public static async Task<T> GetContentAsAsync<T>(this HttpResponseMessage response)
        {
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // Convert collections
            if (typeof(T).IsGenericType &&
                (
                    typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                    typeof(T).GetGenericTypeDefinition() == typeof(ICollection<>) ||
                    typeof(T).GetGenericTypeDefinition() == typeof(List<>)
                ) &&
                typeof(T).GenericTypeArguments.Any())
            {
                if (!responseString.Contains("\"@odata")) return JsonConvert.DeserializeObject<T>(responseString);

                // Get value from the content
                responseString = JObject.Parse(responseString)["value"].ToString();

                var collectionType = typeof(ICollection<>);
                var genericType = collectionType.MakeGenericType(typeof(T).GenericTypeArguments[0]);

                return (T)JsonConvert.DeserializeObject(responseString, genericType);
            }

            // Convert any type not collection
            return JsonConvert.DeserializeObject<T>(responseString);
        }
                        
        public static T GetContentAs<T>(this HttpResponseMessage response)
        {
            return GetContentAsAsync<T>(response).GetAwaiter().GetResult();
        }
        #endregion

        public static DbSet<T> MockDbSet<T>(this IEnumerable<T> data) where T : class, new()
        {
            var mock = data.AsQueryable().BuildMockDbSet();
            return mock.Object;
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
