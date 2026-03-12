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
using System.Threading;

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
        private static TestingProfile s_currentTestingProfile;
        private IConfigurationRoot _configuration;
        private Mock<IMindedExampleContext> _mockIMindedExampleContext;
        private ServiceProvider _serviceProvider;
        private Seeder _seeder;

        public BaseE2ETest()
        {
            // Create the client once in the constructor
            // This ensures we reuse the same WebApplicationFactory and DI container across all tests
            // which is important for SQLite in-memory with Singleton DbContext
            _sutClient = CreateTestApplication();
        }

        [TestInitialize]
        public async Task BaseTestTestInitialize()
        {
            // Reset the database before each test
            // This ensures each test starts with a clean database
            await ResetDb();
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
        protected async Task<T> SeedOne<T>(Expression<Func<T, int>> id) where T : class, new()
        {
            return (await Seed<T>(id, 1, default)).First();
        }

        protected async Task<T> SeedOne<T>(Expression<Func<T, int>> id, Action<T, int> buildAction = default) where T : class, new()
        {
            return (await Seed<T>(id, 1, buildAction)).First();
        }

        protected async Task<IEnumerable<T>> Seed<T>(Expression<Func<T, int>> id) where T : class, new()
        {
            return await Seed<T>(id, 100, default);
        }

        protected async Task<IEnumerable<T>> Seed<T>(Expression<Func<T, int>> id, int quantity = 100) where T : class, new()
        {
            return await Seed<T>(id, quantity, default);
        }

        protected async Task<IEnumerable<T>> Seed<T>(Expression<Func<T, int>> id, int quantity = 100, Action<T, int> buildAction = default) where T : class, new()
        {
            return await _seeder.Seed(id, quantity, buildAction);
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
            s_currentTestingProfile = (TestingProfile) Enum.Parse(typeof(TestingProfile), _configuration.GetValue<string>("TestingProfile"));

            // Setup mocked environment object
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.SetupProperty(p => p.EnvironmentName, s_currentTestingProfile.ToString());
            mockEnv.SetupProperty(p => p.ApplicationName, GetType().Assembly.FullName);
            mockEnv.SetupProperty(p => p.ContentRootPath, AppContext.BaseDirectory);
            mockEnv.SetupProperty(p => p.WebRootPath, AppContext.BaseDirectory);
            IWebHostEnvironment env = mockEnv.Object;

            _mockIMindedExampleContext = new Mock<IMindedExampleContext>(MockBehavior.Strict);
            SetupDbContextMockObject();

            WebApplicationFactory<Startup> applicationFactory = new WebApplicationFactory<Startup>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services => {
                    ConfigureContext(services);

                    // Execute overrides as passed in the test
                    serviceCollectionSetup?.Invoke(services);

                    _serviceProvider = services.BuildServiceProvider();
                });

                builder.UseConfiguration(_configuration);

                // Set the environment to the testing profile to prevent automatic seeding in Startup.cs
                // This ensures that Startup.SeedDatabaseForDevelopment() is not called during tests
                builder.UseEnvironment(s_currentTestingProfile.ToString());
            });

            HttpClient client = applicationFactory.CreateClient();
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
            _mockIMindedExampleContext.SetupGet(t => t.Users).Returns(new List<User>().GetMockDbSet().Object);
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
            switch (s_currentTestingProfile)
            {
                case TestingProfile.UnitTesting:
                    services.OverrideAddScoped(_mockIMindedExampleContext.Object);
                    _seeder = new Seeder(s_currentTestingProfile, _mockIMindedExampleContext.Object, _mockIMindedExampleContext);
                    break;
                case TestingProfile.E2ELive:
                    // Remove any existing DbContext registrations from Startup.cs
                    ServiceDescriptor descriptorDbContext = services.SingleOrDefault(d => d.ServiceType == typeof(MindedExampleContext));
                    if (descriptorDbContext != null)
                    {
                        services.Remove(descriptorDbContext);
                    }

                    ServiceDescriptor descriptorIMindedContext = services.SingleOrDefault(d => d.ServiceType == typeof(IMindedExampleContext));
                    if (descriptorIMindedContext != null)
                    {
                        services.Remove(descriptorIMindedContext);
                    }

                    // Use SQLite in memory database for testing
                    // Register DbContext as Singleton to keep the in-memory database alive
                    services.AddDbContext<MindedExampleContext>(options =>
                    {
                        options.UseSqlite($"DataSource='file::memory:?cache=shared'");
                        //options.UseSqlite("DataSource=:memory:");
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    }, ServiceLifetime.Singleton);

                    services.AddSingleton<IMindedExampleContext>(s =>
                    {
                        MindedExampleContext context = s.GetService<MindedExampleContext>();
                        _connection = context.Database.GetDbConnection();
                        _connection.Open(); // Keep connection open to maintain in-memory database
                        context.Database.EnsureCreated();

                        // Only set _context and _seeder if they haven't been set yet
                        // DO NOT seed the database here - let the tests control seeding
                        if (_context == null)
                        {
                            _context = context;
                            _seeder = new Seeder(s_currentTestingProfile, _context, _mockIMindedExampleContext);
                        }

                        return context;
                    });
                    break;
                case TestingProfile.E2E:
                    // Remove any existing DbContext registrations from Startup.cs
                    ServiceDescriptor descriptorDbContextE2E = services.SingleOrDefault(d => d.ServiceType == typeof(MindedExampleContext));
                    if (descriptorDbContextE2E != null)
                    {
                        services.Remove(descriptorDbContextE2E);
                    }

                    ServiceDescriptor descriptorIMindedContextE2E = services.SingleOrDefault(d => d.ServiceType == typeof(IMindedExampleContext));
                    if (descriptorIMindedContextE2E != null)
                    {
                        services.Remove(descriptorIMindedContextE2E);
                    }

                    services.AddDbContext<MindedExampleContext>(options =>
                    {
                        options.UseSqlServer(_configuration.GetConnectionString(Constants.ConfigConnectionStringName));
                        options.UseLoggerFactory(Startup.AppLoggerFactory);
                    });

                    services.AddTransient<IMindedExampleContext>(s =>
                    {
                        _context = s.GetService<MindedExampleContext>();
                        _context.Database.EnsureCreated();
                        // DO NOT seed the database here - let the tests control seeding
                        _seeder = new Seeder(s_currentTestingProfile, _context, _mockIMindedExampleContext);
                        return _context;
                    });
                    break;
            }
        }

        /// <summary>
        /// Drop and create the testing database, this does not have effect for UnitTesting
        /// </summary>
        public async Task ResetDb(CancellationToken cancellationToken = default)
        {
            if (s_currentTestingProfile == TestingProfile.UnitTesting)
            {
                _mockIMindedExampleContext.Reset();
                SetupDbContextMockObject();
                _seeder = new Seeder(s_currentTestingProfile, _mockIMindedExampleContext.Object, _mockIMindedExampleContext);
                return;
            }

            if (s_currentTestingProfile == TestingProfile.E2ELive)
            {
                // For SQLite in-memory with cache=shared, we need to clear all data between tests
                // while keeping the connection open to maintain the in-memory database

                // IMPORTANT: We use a different strategy than EnsureDeleted/EnsureCreated because:
                // 1. The DbContext is a singleton, so its internal caches persist
                // 2. Dropping and recreating the schema can cause EF Core to cache stale metadata
                // 3. Simply deleting data is faster and avoids cache invalidation issues
                // 4. We must NOT dispose and recreate the context because it's a singleton that's
                //    already injected into the application controllers

                if (_context is MindedExampleContext concreteContext)
                {
                    // Clear the change tracker first to avoid tracking issues
                    concreteContext.ChangeTracker.Clear();

                    // Delete all data from tables in the correct order (respecting foreign keys)
                    // We must delete in reverse order of dependencies to avoid FK constraint violations
                    // Using raw SQL is more efficient than loading all entities into memory

                    // Get table names from the EF Core model to avoid hardcoding
                    string transactionsTable = concreteContext.Model.FindEntityType(typeof(Transaction))?.GetTableName() ?? "Transactions";
                    string categoriesTable = concreteContext.Model.FindEntityType(typeof(Category))?.GetTableName() ?? "Categories";
                    string usersTable = concreteContext.Model.FindEntityType(typeof(User))?.GetTableName() ?? "Users";

                    // Delete child entities first (Transactions depend on Categories and Users)
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                    await concreteContext.Database.ExecuteSqlRawAsync($"DELETE FROM {transactionsTable}", cancellationToken);
                    await concreteContext.Database.ExecuteSqlRawAsync($"DELETE FROM {categoriesTable}", cancellationToken);
                    await concreteContext.Database.ExecuteSqlRawAsync($"DELETE FROM {usersTable}", cancellationToken);
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.

                    // Reset the auto-increment counters for SQLite
                    // This ensures IDs start from 1 for each test, making tests more predictable
                    await concreteContext.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence", cancellationToken);

                    // Clear the change tracker again after deletions to ensure clean state
                    concreteContext.ChangeTracker.Clear();
                }

                // Recreate the seeder with the same context (which is a singleton)
                _seeder = new Seeder(s_currentTestingProfile, _context, _mockIMindedExampleContext);
                return;
            }

            await _context.Database.EnsureDeletedAsync(cancellationToken);
            await _context.Database.EnsureCreatedAsync(cancellationToken);

            // Recreate the seeder with the refreshed context
            _seeder = new Seeder(s_currentTestingProfile, _context, _mockIMindedExampleContext);
        }
    }
}
