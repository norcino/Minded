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
using Moq;
using System.Linq;
using System.Threading.Tasks;
using Data.Entity;
using System.Linq.Expressions;
using Builder;
using FluentAssertions;
using Common.Tests;
using Common.Configuration;
using Application.Api;
using System.Reflection;
using AnonymousData;
using System.Threading;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Data.Common;

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
        private TestServer _server;
        private IServiceCollection _serviceCollection;
        private DbConnection _connection;
        private IMindedExampleContext _context;
        private TestingProfile _currentTestingProfile;
        private IConfigurationRoot _configuration;
        private Mock<IMindedExampleContext> _mockIMindedExampleContext;

        public BaseE2ETest()
        {
            _sutClient = CreateServer().CreateClient();
        }

        [TestInitialize]
        public void BaseTestTestInitialize()
        {
            ResetDb();
        }

        //[TestCleanup]
        //public void BaseTestTestCleanup()
        //{
        //    _server.Dispose();
        //    _sutClient.Dispose();
        //}

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

        // TODO handle composite key
        // TODO add seed single entity
        protected T SeedOne<T>(Expression<Func<T, int>> id) where T : class, new()
        {
            return Seed<T>(id, 1, default).First(); ;
        }

        protected IEnumerable<T> Seed<T>(Expression<Func<T, int>> id) where T : class, new()
        {
            return Seed<T>(id, 100, default);
        }

        protected IEnumerable<T> Seed<T>(Expression<Func<T, int>> id, int quantity = 100) where T : class, new()
        {
            return Seed<T>(id, quantity, default);
        }

        // TODO Convert int in TP and use new Builder function to generate a value
        protected IEnumerable<T> Seed<T>(Expression<Func<T, int>> id, int quantity = 100, Action<T, int> buildAction = default) where T : class, new()
        {
            List<T> entities = null;

            if (_currentTestingProfile == TestingProfile.UnitTesting)
            {
                entities = Builder<T>.New().BuildMany(quantity, (e,i) =>
                {
                    // Execute custom action initialization if present
                    if (buildAction != default)
                        buildAction(e, i);

                    // Set the primary key
                    SetPrimaryKey(id, e);
                });
                var property = _context.GetType().GetProperties()
                    .First(p =>
                        p.PropertyType.IsGenericType &&
                        p.PropertyType == typeof(DbSet<T>));

                var parameter = Expression.Parameter(typeof(IMindedExampleContext));
                var body = Expression.PropertyOrField(parameter, property.Name);
                var lambdaExpression = Expression.Lambda<Func<IMindedExampleContext, DbSet<T>>>(body, parameter);

                var mockDbSet = entities.GetMockDbSet();

                mockDbSet.Setup(s => s.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                    .Callback((T added, CancellationToken ct) =>
                {
                    // Create the setter to simulate the creation of the ID in the entity
                    SetPrimaryKey(id, added);

                    var currentEntities = mockDbSet.Object.ToList<T>();
                    currentEntities.Add(added);
                    mockDbSet = currentEntities.GetMockDbSet();
                    _mockIMindedExampleContext.SetupGet(lambdaExpression).Returns(mockDbSet.Object);
                });

                _mockIMindedExampleContext.SetupGet(lambdaExpression).Returns(mockDbSet.Object);
            }
            else if (_currentTestingProfile == TestingProfile.E2ELive)
            {
                entities = Builder<T>.New().BuildMany(quantity, (e,i) => {
                    // Execute custom action initialization if present
                    if (buildAction != default)
                        buildAction(e, i);

                    // Set the primary key
                    SetPrimaryKey(id, e);
                });
                var property = _context.GetType().GetProperties()
                    .First(p =>
                        p.PropertyType.IsGenericType &&
                        p.PropertyType == typeof(DbSet<T>));

                DbSet<T> dbSet = (DbSet<T>)property.GetValue(_context);
                dbSet.AddRange(entities);
                _context.SaveChanges();
            }
            else // E2E
            {
                entities = Builder<T>.New().BuildMany(quantity, buildAction);
                var property = _context.GetType().GetProperties()
                    .First(p =>
                        p.PropertyType.IsGenericType &&
                        p.PropertyType == typeof(DbSet<T>));

                DbSet<T> dbSet = (DbSet<T>)property.GetValue(_context);
                dbSet.AddRange(entities);
                _context.SaveChanges();
            }

            return entities;
        }

        private static void SetPrimaryKey<T>(Expression<Func<T, int>> id, T e) where T : class, new()
        {
            var parameter1 = Expression.Parameter(typeof(T));
            var parameter2 = Expression.Parameter(typeof(int));

            var member = (MemberExpression)id.Body;
            var propertyInfo = (PropertyInfo)member.Member;

            var property = Expression.Property(parameter1, propertyInfo);
            var assignment = Expression.Assign(property, parameter2);

            var setter = Expression.Lambda<Action<T, int>>(assignment, parameter1, parameter2);

            setter.Compile()(e, Any.Int());
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
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("testappsettings.json", optional: false)
                .AddInMemoryCollection(configurationOverride)
                .Build();

            // Load the configured testing profile
            _currentTestingProfile = (TestingProfile)Enum.Parse(typeof(TestingProfile), _configuration.GetValue<string>("TestingProfile"));

            // Setup mocked environment object
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.SetupProperty(p => p.EnvironmentName, _currentTestingProfile.ToString());
            mockEnv.SetupProperty(p => p.ApplicationName, GetType().Assembly.FullName);
            mockEnv.SetupProperty(p => p.ContentRootPath, AppContext.BaseDirectory);
            mockEnv.SetupProperty(p => p.WebRootPath, AppContext.BaseDirectory);
            var env = mockEnv.Object;

            ServiceProvider serviceProvider = null;
            _mockIMindedExampleContext = new Mock<IMindedExampleContext>(MockBehavior.Strict);
            SetupDbContextMockObject();

            var applicationStartup = new Startup(_configuration, env);
            var builder = new WebHostBuilder().UseConfiguration(_configuration);

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

            _server = new TestServer(builder);

            _context = serviceProvider.GetService<IMindedExampleContext>();

            return _server;
        }

        /// <summary>
        /// Prototyping
        /// </summary>
        private void SetupDbContextMockObject()
        {
            _mockIMindedExampleContext.Setup(c => c.Dispose());
            _mockIMindedExampleContext.Setup<Task<int>>(c => c.SaveChangesAsync()).ReturnsAsync(1);

            var c = new List<Category>().GetMockDbSet();
            var t = new List<Transaction>().GetMockDbSet();

            c.Setup(s => s.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>())).Callback((Category added, CancellationToken ct) =>
            {                
                // TODO
                //added.Id = Any.Int();
            });
            
            _mockIMindedExampleContext.SetupGet(c => c.Categories).Returns(c.Object);
            _mockIMindedExampleContext.SetupGet(c => c.Transactions).Returns(t.Object);
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
                    break;
                case TestingProfile.E2ELive:
                    // Use SQLite in memory database for testing
                    services.AddDbContext<MindedExampleContext>(options =>
                    {
                        options.UseSqlite($"DataSource='file::memory:?cache=shared'");
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    });

                    // Use singleton context when using SQLite in memory if the connection is closed the database is going to be destroyed
                    // so must use a singleton context, open the connection and manually close it when disposing the context
                    services.AddSingleton<IMindedExampleContext>(s =>
                    {
                        _context = s.GetService<MindedExampleContext>();
                        //_context.Database.OpenConnection();
                        _connection = _context.Database.GetDbConnection();
                        _context.Database.EnsureCreated();
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
                _context.Database.OpenConnection();
                _connection = _context.Database.GetDbConnection(); // TODO DB Never destroyed
                _context.Database.EnsureCreated();
                return;
            }
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }
    }
}
