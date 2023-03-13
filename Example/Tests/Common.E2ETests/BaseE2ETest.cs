using Data.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Moq;
using System.Threading.Tasks;
using Data.Entity;
using Common.Tests;
using Common.Configuration;
using Application.Api;
using System.Data.Common;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using QM.Common.Testing;
using System.Linq.Expressions;
using System.Linq;

namespace Common.E2ETests
{
    /// <summary>
    /// 1) Real SQL Server - CI Testing
    /// 2) In Memory SQLite - Dev/Live Testing
    /// 3) Mock - Unit testing
    /// </summary>
    [TestClass]
    public abstract class BaseE2ETest
    {
        protected const int MaxPageItemNumber = 100;
        protected HttpClient _sutClient;
        private IServiceCollection _serviceCollection;
        private DbConnection _connection;
        private IMindedExampleContext _context;
        private static TestingProfile _currentTestingProfile;
        private IConfigurationRoot _configuration;
        private Mock<IMindedExampleContext> _mockIMindedExampleContext;
        private ServiceProvider _serviceProvider;
        private Seeder _seeder;

        public BaseE2ETest()
        {
            _sutClient = CreateTestApplication();
        }

        [TestInitialize]
        public void BaseTestTestInitialize()
        {
            ResetDb();
        }

        [TestCleanup]
        public void BaseTestTestCleanup()
        {
            _sutClient.Dispose();
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
        protected T SeedOne<T>(Expression<Func<T, int>> id) where T : class, new()
        {
            return Seed<T>(id, 1, default).First();
        }

        protected IEnumerable<T> Seed<T>(Expression<Func<T, int>> id) where T : class, new()
        {
            return Seed<T>(id, 100, default);
        }

        protected IEnumerable<T> Seed<T>(Expression<Func<T, int>> id, int quantity = 100) where T : class, new()
        {
            return Seed<T>(id, quantity, default);
        }

        
        protected IEnumerable<T> Seed<T>(Expression<Func<T, int>> id, int quantity = 100, Action<T, int> buildAction = default) where T : class, new()
        {
            return _seeder.Seed(id, quantity, buildAction);
        }

        /// <summary>
        /// Create the HttpClient with the possibility to customize the service collection setup and custom configuration override
        /// </summary>
        /// <param name="resetDd">Allow to specify if the database must be reset, default is true</param>
        /// <param name="serviceCollectionSetup">The service collection action to customize</param>
        /// <param name="configurationOverride">Custom configuration which will override the config file</param>
        /// <returns>HttpClient</returns>
        protected HttpClient CreateTestApplication(Action<IServiceCollection> serviceCollectionSetup = null, Dictionary<string, string> configurationOverride = null)
        {
            // Load application configuration from the test folder
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("testappsettings.json", optional: false)
                .AddInMemoryCollection(configurationOverride)
                .Build();

            // Load the configured testing profile
            _currentTestingProfile = (TestingProfile) Enum.Parse(typeof(TestingProfile), _configuration.GetValue<string>("TestingProfile"));

            // Setup mocked environment object
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.SetupProperty(p => p.EnvironmentName, _currentTestingProfile.ToString());
            mockEnv.SetupProperty(p => p.ApplicationName, GetType().Assembly.FullName);
            mockEnv.SetupProperty(p => p.ContentRootPath, AppContext.BaseDirectory);
            mockEnv.SetupProperty(p => p.WebRootPath, AppContext.BaseDirectory);
            var env = mockEnv.Object;

            _mockIMindedExampleContext = new Mock<IMindedExampleContext>(MockBehavior.Strict);
            SetupDbContextMockObject();

            var applicationFactory = new WebApplicationFactory<Startup>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services => {
                    ConfigureContext(services);

                    // Execute overrides as passed in the test
                    serviceCollectionSetup?.Invoke(services);

                    _serviceProvider = services.BuildServiceProvider();
                });

                builder.UseConfiguration(_configuration);
            });

            var client = applicationFactory.CreateClient();
            _context = _serviceProvider.GetService<IMindedExampleContext>();

            return client;
        }

        /// <summary>
        /// Prototyping
        /// </summary>
        private void SetupDbContextMockObject()
        {
            _mockIMindedExampleContext.Setup(c => c.Dispose());
            _mockIMindedExampleContext.Setup<Task<int>>(c => c.SaveChangesAsync()).ReturnsAsync(1);

            _mockIMindedExampleContext.SetupGet(c => c.Categories).Returns(new List<Category>().GetMockDbSet().Object);
            _mockIMindedExampleContext.SetupGet(t => t.Transactions).Returns(new List<Transaction>().GetMockDbSet().Object);
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
            _serviceCollection = services;
            switch (_currentTestingProfile)
            {
                case TestingProfile.UnitTesting:
                    services.OverrideAddScoped(_mockIMindedExampleContext.Object);
                    _seeder = new Seeder(_currentTestingProfile, _mockIMindedExampleContext.Object, _mockIMindedExampleContext);
                    break;
                case TestingProfile.E2ELive:
                    // Use SQLite in memory database for testing
                    services.AddDbContext<MindedExampleContext>(options =>
                    {
                        options.UseSqlite($"DataSource='file::memory:?cache=shared'");
                        //options.UseSqlite("DataSource=:memory:");
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    });

                    services.AddScoped<IMindedExampleContext>(s =>
                    {
                        _context = s.GetService<MindedExampleContext>();
                        _connection = _context.Database.GetDbConnection();
                        _context.Database.EnsureCreated();
                        _seeder = new Seeder(_currentTestingProfile, _context, _mockIMindedExampleContext);
                        return _context;
                    });
                    break;
                case TestingProfile.E2E:
                    services.AddDbContext<MindedExampleContext>(options =>
                    {
                        options.UseSqlServer(_configuration.GetConnectionString(Constants.ConfigConnectionStringName));
                        options.UseLoggerFactory(Startup.AppLoggerFactory);
                    });

                    services.AddTransient<IMindedExampleContext>(s =>
                    {
                        _context = s.GetService<MindedExampleContext>();
                        _context.Database.EnsureCreated();
                        _seeder = new Seeder(_currentTestingProfile, _context, _mockIMindedExampleContext);
                        return _context;
                    });
                    break;
            }
        }

        /// <summary>
        /// Drop and create the testing database, this does not have effect for UnitTesting
        /// </summary>
        public void ResetDb()
        {
            if (_currentTestingProfile == TestingProfile.UnitTesting)
            {
                _mockIMindedExampleContext.Reset();
                SetupDbContextMockObject();
                return;
            }

            if (_currentTestingProfile == TestingProfile.E2ELive)
            {
                _connection.Close();
                _connection.Dispose();

                _context.Dispose();

                _context = _serviceProvider.GetService<IMindedExampleContext>();

                _context.Database.OpenConnection();
                _connection = _context.Database.GetDbConnection(); // TODO DB Never destroyed

                _context.Database.EnsureDeleted();
                _context.Database.EnsureCreated();
                return;
            }

            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }
    }
}
