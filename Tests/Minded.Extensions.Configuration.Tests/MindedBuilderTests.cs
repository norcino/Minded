using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Caching.Abstractions.Decorator;
using Minded.Extensions.Caching.Abstractions.Tests;
using Minded.Framework.Decorator;
using Moq;

namespace Minded.Extensions.Configuration.Tests
{
    [TestClass]
    public class MindedBuilderTests
    {
        #if RELEASE
        [TestMethod]
        public void TheInstatioationOfMindedBuilder_ShouldAutomatically_InvokeAttributeValidators_OnlyInDebugMode()
        {
            var serviceCollection = new ServiceCollection();
            var configuration = new Mock<IConfiguration>().Object;
            var assemblyFilter = new Func<AssemblyName, bool>(name =>
                name.Name == "Minded.Extensions.Caching.Abstractions" ||
                name.Name == "Minded.Testing.Common"
            );

            var assembly = Assembly.GetAssembly(typeof(IDecoratingAttributeValidator));

            var queryUsedForTheTest = new InvalidTestQueryWithCachingAttribute();
            Assert.IsNotNull(queryUsedForTheTest);

            Assert.IsNotNull(new MindedBuilder(serviceCollection, configuration, assemblyFilter));
        }
        #endif

        [TestMethod]
        public void TheInstatioationOfMindedBuilder_ShouldAutomatically_InvokeAttributeValidators()
        {
            var serviceCollection = new ServiceCollection();
            var configuration = new Mock<IConfiguration>().Object;
            var assemblyFilter = new Func<AssemblyName, bool>(name =>
                name.Name == "Minded.Extensions.Caching.Abstractions" ||
                name.Name == "Minded.Testing.Common"
            );

            var assembly = Assembly.GetAssembly(typeof(IDecoratingAttributeValidator));

            var queryUsedForTheTest = new InvalidTestQueryWithCachingAttribute();
            Assert.IsNotNull(queryUsedForTheTest);

            try
            {
                var _ = new MindedBuilder(serviceCollection, configuration, assemblyFilter);
            }
            catch(InvalidOperationException ex)
            {
                Assert.AreEqual("The class Minded.Extensions.Caching.Abstractions.Tests.InvalidTestQueryWithCachingAttribute has a CacheAttribute (or a derived class) but does not implement IGenerateCacheKey.", ex.Message);
                return;
            }

            Assert.Fail($"The constructor of the MindedBulder is supposed to find the {nameof(CachingDecoratorAttributeValidator)} and fail the validation for {nameof(InvalidTestQueryWithCachingAttribute)}.");
        }

        [TestMethod]
        public void TheInstatioationOfMindedBuilder_ShouldAutomatically_InvokeAttributeValidators_IgnoringAssembliesNotInFilter()
        {
            var serviceCollection = new ServiceCollection();
            var configuration = new Mock<IConfiguration>().Object;
            var assemblyFilter = new Func<AssemblyName, bool>(name =>
                name.Name == "Minded.Extensions.Caching.Abstractions"
                // The failing class is located in this assebly: "Minded.Testing.Common"
            );

            var assembly = Assembly.GetAssembly(typeof(IDecoratingAttributeValidator));

            var queryUsedForTheTest = new InvalidTestQueryWithCachingAttribute();
            Assert.IsNotNull(queryUsedForTheTest);

            Assert.IsNotNull(new MindedBuilder(serviceCollection, configuration, assemblyFilter));
        }
    }
}
