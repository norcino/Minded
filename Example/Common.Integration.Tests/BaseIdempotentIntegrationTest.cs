using System;
using System.Data;
using Common.Configuration;
using Data.Context;
using Minded.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Application.Api;
using Microsoft.Extensions.Logging;

namespace Common.IntegrationTests
{
    [TestClass]
    public abstract class BaseIdempotentIntegrationTest
    {
        protected IDbContextTransaction Transaction;        
        protected IMindedExampleContext _context;
        protected IMindedExampleContext Context
        {
            get
            {
                if (_context != null) return _context;
                _context = ServiceProvider.GetService<IMindedExampleContext>();
                return _context;
            }
        }
        protected ServiceProvider ServiceProvider { get; private set; }

        public BaseIdempotentIntegrationTest()
        {
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddLogging(logging =>
            {
                logging.AddConfiguration(configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
            });
            serviceCollection.AddMinded(assembly => assembly.Name.StartsWith("Service."));
            
            Startup.RegisterContext(serviceCollection, null);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            // Context provider is used by the Persisters
            // This need to be the same used by the idempotent tests or the database will
            // be locked by the transactions
            // ContextProvider.ServiceProvider = ServiceProvider;
        }

        [TestInitialize]
        public void Initialize()
        {
            Transaction = Context.Database.BeginTransaction(IsolationLevel.ReadCommitted);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Transaction?.Rollback();
        }
    }
}
