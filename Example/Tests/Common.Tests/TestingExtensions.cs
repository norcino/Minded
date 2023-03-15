using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using FluentAssertions.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using FluentAssertions;
using Minded.Framework.CQRS.Abstractions;

namespace QM.Common.Testing
{
    public static class DbMockingExtensions
    {
        public static DbSet<T> MockDbSet<T>(this IQueryable<T> queryable) where T : class, new()
        {
            return queryable.GetMockDbSet().Object;
        }

        public static Mock<DbSet<T>> GetMockDbSet<T>(this IQueryable<T> queryable) where T : class, new()
        {
            var dbSet = queryable.AsQueryable<T>().BuildMockDbSet();

            dbSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>((s) => {
                queryable = queryable.Append(s);
            });

            dbSet.Setup(d => d.AddRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>((l) => {
                foreach (var e in l)
                {
                    queryable = queryable.Append(e);
                }
            });

            dbSet.Setup(d => d.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>())).Callback<T, CancellationToken>((s, c) => {
                queryable = queryable.Append(s);
            });

            dbSet.Setup(d => d.AddRangeAsync(It.IsAny<IEnumerable<T>>(), It.IsAny<CancellationToken>())).Callback<IEnumerable<T>, CancellationToken>((l, c) => {
                foreach (var e in l)
                {
                    queryable = queryable.Append(e);
                }
            });

            dbSet.Setup(d => d.RemoveRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>((s) => {
                throw new Exception("Warning this method supports ready only functionalities");
            });

            dbSet.Setup(d => d.Remove(It.IsAny<T>())).Callback<T>((s) => {
                throw new Exception("Warning this method supports ready only functionalities");
            });
            return dbSet;
        }

        public static DbSet<T> MockDbSet<T>(this List<T> enumerable) where T : class
        {
            return enumerable.GetMockDbSet().Object;
        }

        /// <summary>
        /// This method allows to get the DbSet which can be used to mock a DbContext.
        /// This method extends List instead of IEnumberable, as the latter doesn't allow write operations and
        /// if you are testing Add (creations), you won't be able to assert that the new element has been added to the DbSet
        /// </summary>
        /// <typeparam name="T">Entity Type</typeparam>
        /// <param name="sourceList">List used to simulated the data stored in the DbSet</param>
        /// <returns>Testable DbSet</returns>
        public static Mock<DbSet<T>> GetMockDbSet<T>(this List<T> enumerable) where T : class
        {
            var dbSet = enumerable.AsQueryable<T>().BuildMockDbSet();

            dbSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>((e) => {
                enumerable.Add(e);
            });

            dbSet.Setup(d => d.AddRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>((l) => {
                enumerable.AddRange(l);
            });

            dbSet.Setup(d => d.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>())).Callback<T, CancellationToken>((e, c) => {
                enumerable.Add(e);
            });

            dbSet.Setup(d => d.AddRangeAsync(It.IsAny<IEnumerable<T>>(), It.IsAny<CancellationToken>())).Callback<IEnumerable<T>, CancellationToken>((l, c) => {
                enumerable.AddRange(l);
            });

            dbSet.Setup(d => d.Remove(It.IsAny<T>())).Callback<T>((e) =>
            {
                enumerable.Remove(e);
            });

            dbSet.Setup(d => d.RemoveRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>((l) =>
            {
                enumerable.RemoveAll(e => l.Contains(e));
            });
            return dbSet;
        }
    }

    public static class TestingExtensions
    {
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

        #region CommandResponse
        public static void ContainOutcomeEntry(this HttpResponseMessageAssertions assertion, string expactedMessage, string expectedProperty, Severity? severity = null)
        {
            var content = assertion.Subject.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var outcomeEntriesJson = JObject.Parse(content)["outcomeEntries"].ToString();
            var outcomeEntries = JsonSerializer.Deserialize<List<OutcomeEntry>>(outcomeEntriesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            foreach (var entry in outcomeEntries.Where(o => o.Message == expactedMessage && o.PropertyName == expectedProperty))
            {
                entry.Message.Should().Be(expactedMessage);
                entry.PropertyName.Should().Be(expectedProperty);

                if (severity != null)
                    entry.Severity.Should().Be(severity);
            }
        }
        #endregion
    }
}
