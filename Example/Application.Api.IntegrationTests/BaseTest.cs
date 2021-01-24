using Data.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Common.Tests;
using Common.Configuration;
using Moq;
using Data.Common.Testing.Builder;
using System.Linq;
using System.Threading.Tasks;
using Data.Entity;
using System.Linq.Expressions;
using Builder;
using FluentAssertion.MSTest;

namespace Application.Api.IntegrationTests
{
    public enum TestingProfile
    {
        /// <summary>
        /// Specify that the testing profile must support unit testing mocking the database
        /// </summary>
        UnitTesting = 0,

        /// <summary>
        /// In memory SQLite database is used to support live testing in Dev environments
        /// </summary>
        E2ELive = 1,
        
        /// <summary>
        /// End to end testing targeting real database for CI (using LocalDB) or Automation testing
        /// </summary>
        E2E = 2
    }

    /// <summary>
    /// 1) Real SQL Server - CI Testing
    /// 2) In Memory SQLite - Dev/Live Testing
    /// 3) Mock - Unit testing
    /// </summary>
    [TestClass]
    public class BaseTest
    {
        protected HttpClient Client;
        protected TestServer Server;
        protected IMindedExampleContext Context;
        private TestingProfile TestingProfile;
        private IConfigurationRoot Config;
        private Mock<IMindedExampleContext> mockIMindedExampleContext;

        [TestInitialize]
        public void TestInitialize()
        {
            Client = CreateServer().CreateClient();
            ResetDb();
        }

        [TestMethod]
        public async Task Get_all_Categories_should_return_200Ok_and_All_existing_categories()
        {
            var expectedCategories = Seed<Category>((c, i) => c.Id = i, 10);
            
            var response = await Client.GetAsync("/api/category");

            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccessStatusCode);

            var categories = await response.GetContentAsAsync<List<Category>>();
            Assert.AreEqual(expectedCategories.Count(), categories.Count);
            // TODO are the same as the src
        }

        [TestCleanup]
        public virtual void BaseTestCleanup()
        {
            Server.Dispose();
            Client.Dispose();
        }

        /// <summary>
        /// This method is used to Seed data consumed by the tested application and components.
        /// Passing a custom action is possible do describe how each entity should be created.
        /// This method will also return the newly created data.
        /// Based on the testing profile, this method will:
        /// - UnitTesting: Mock the context DbSet for the given type using the created data
        /// - E2ELive: Insert the created data into the database
        /// - E2E: Insert the created data into the database
        /// Note that this method will not save or mock child entities even if provided in the build action.
        /// Make sure the data is correctly mocked or persisted calling this method for each entity type in the right order.
        /// </summary>
        /// <typeparam name="T">Type of the entity to be created</typeparam>
        /// <param name="buildAction">Action to customize the creation of the entities</param>
        /// <param name="quantity">Number of entities to be created</param>
        /// <returns>List of created entities</returns>
        protected IEnumerable<T> Seed<T>(Action<T, int> buildAction, int quantity = 100) where T : class, new()
        {
            List<T> entities = null;

            if (TestingProfile == TestingProfile.UnitTesting)
            {
                entities = Builder<T>.New().BuildMany(quantity, buildAction);
                var property = Context.GetType().GetProperties()
                    .First(p =>
                        p.PropertyType.IsGenericType &&
                        p.PropertyType == typeof(DbSet<T>));

                var parameter = Expression.Parameter(typeof(IMindedExampleContext));
                var body = Expression.PropertyOrField(parameter, property.Name);
                var lambdaExpression = Expression.Lambda<Func<IMindedExampleContext, DbSet<T>>>(body, parameter);
                                
                mockIMindedExampleContext.SetupGet(lambdaExpression).Returns(entities.MockDbSet());
            }
            else if (TestingProfile == TestingProfile.E2ELive)
            {
                entities = Builder<T>.New().BuildMany(quantity, buildAction);
                var property = Context.GetType().GetProperties()
                    .First(p =>
                        p.PropertyType.IsGenericType &&
                        p.PropertyType == typeof(DbSet<T>));

                DbSet<T> dbSet = (DbSet<T>)property.GetValue(Context);
                dbSet.AddRange(entities);
                Context.SaveChanges();
            }
            else
            {
                entities = Persister<T>.New(Context).Persist(quantity, buildAction);
                var property = Context.GetType().GetProperties()
                    .First(p =>
                        p.PropertyType.IsGenericType &&
                        p.PropertyType == typeof(DbSet<T>));
            }

            return entities;
        }

        /// <summary>
        /// Create the Server with the possibility to customize the service collection setup and custom configuration override
        /// </summary>
        /// <param name="resetDd">Allow to specify if the database must be reset, default is true</param>
        /// <param name="serviceCollectionSetup">The service collection action to customize</param>
        /// <param name="configurationOverride">Custom configuration which will override the config file</param>
        /// <returns>TestServer</returns>
        protected TestServer CreateServer(Action<IServiceCollection> serviceCollectionSetup = null,
            Dictionary<string, string> configurationOverride = null)
        {
            // Load application configuration from the test folder
            Config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("testappsettings.json", optional: false)
                .AddInMemoryCollection(configurationOverride)
                .Build();

            // Load the configured testing profile
            TestingProfile = (TestingProfile) Enum.Parse(typeof(TestingProfile), Config.GetValue<string>("TestingProfile"));

            // Setup mocked environment object
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.SetupProperty(p => p.EnvironmentName, TestingProfile.ToString());
            mockEnv.SetupProperty(p => p.ApplicationName, GetType().Assembly.FullName);
            mockEnv.SetupProperty(p => p.ContentRootPath, AppContext.BaseDirectory);
            mockEnv.SetupProperty(p => p.WebRootPath, AppContext.BaseDirectory);
            var env = mockEnv.Object;

            ServiceProvider serviceProvider = null;
            mockIMindedExampleContext = new Mock<IMindedExampleContext>(MockBehavior.Strict);
            mockIMindedExampleContext.Setup(c => c.Dispose());

            var applicationStartup = new Startup(Config, env);
            var builder = new WebHostBuilder().UseConfiguration(Config);
            
            builder.Configure(app =>
            {
                // Application configuration
                applicationStartup.Configure(app);
            });
            builder.ConfigureServices(services =>
            {
                // Application service configuration
                applicationStartup.ConfigureServices(services);

                ConfigureContext(services);

                // Execute overrides as passed in the test
                serviceCollectionSetup?.Invoke(services);

                serviceProvider = services.BuildServiceProvider();
            });

            Server = new TestServer(builder);

            Context = serviceProvider.GetService<IMindedExampleContext>();

            return Server;
        }

        /// <summary>
        /// Configure the DB Context based on the configured TestingProfile
        /// - UnitTesting: Will mock the Db Context
        /// - E2ELive: Will use in memory SQLite database
        /// - E2E: Will user SQL Server or LocalDB depending on the connection string
        /// </summary>
        /// <param name="services">Service collection of the created servcer</param>
        private void ConfigureContext(IServiceCollection services)
        {
            switch (TestingProfile)
            {
                case TestingProfile.UnitTesting:
                    services.OverrideAddScoped(mockIMindedExampleContext.Object);
                    break;
                case TestingProfile.E2ELive:
                    // Use SQLite in memory database for testing
                    services.AddDbContext<MindedExampleContext>(options => options.UseSqlite($"DataSource='file::memory:?cache=shared'"));

                    // Use singleton context when using SQLite in memory if the connection is closed the database is going to be destroyed
                    // so must use a singleton context, open the connection and manually close it when disposing the context
                    services.AddSingleton<IMindedExampleContext>(s =>
                    {
                        Context = s.GetService<MindedExampleContext>();
                        Context.Database.OpenConnection();
                        Context.Database.EnsureCreated();                        
                        return Context;
                    });
                    break;
                case TestingProfile.E2E:
                    services.AddDbContext<MindedExampleContext>(options =>
                    {
                        options.UseSqlServer(Config.GetConnectionString(Constants.ConfigConnectionStringName));
                        options.UseLoggerFactory(Startup.AppLoggerFactory);
                    });

                    services.AddTransient<IMindedExampleContext>(s =>
                    {
                        var context = s.GetService<MindedExampleContext>();
                        context.Database.EnsureCreated();
                        return context;
                    });

                    break;
            }            
        }

        /// <summary>
        /// Drop and create the testing database, this does not have effect for UnitTesting
        /// </summary>
        public virtual void ResetDb()
        {
            if (TestingProfile == TestingProfile.UnitTesting) return;

            Context.Database.EnsureDeleted();
            Context.Database.EnsureCreated();
        }
    }
}
