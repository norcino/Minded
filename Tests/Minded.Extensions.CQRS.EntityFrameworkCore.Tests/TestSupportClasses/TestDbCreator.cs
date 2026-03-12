using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Minded.Extensions.CQRS.EntityFrameworkCore.Tests.TestSupportClasses
{
    /// <summary>
    /// Creates and configures test database contexts with support for multiple database providers.
    /// Supports SQLite in-memory (default), SQLite file-based, and SQL Server/LocalDB.
    /// Configuration is read from testappsettings.json or environment variables.
    /// </summary>
    public class TestDbCreator
    {
        private static readonly object _lock = new();
        private static bool _databaseInitialized;
        private static DbConnection _sharedConnection;
        private readonly IConfigurationRoot _configuration;
        private readonly DatabaseProvider _databaseProvider;
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of TestDbCreator with configuration from testappsettings.json
        /// </summary>
        public TestDbCreator()
        {
            _configuration = LoadConfiguration();
            _databaseProvider = GetDatabaseProvider();
            _connectionString = GetConnectionString();

            InitializeDatabase();
        }

        /// <summary>
        /// Loads configuration from testappsettings.json file
        /// </summary>
        private IConfigurationRoot LoadConfiguration()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("testappsettings.json", optional: false, reloadOnChange: false);

            return configBuilder.Build();
        }

        /// <summary>
        /// Determines the database provider from configuration
        /// Defaults to SQLiteInMemory if not specified
        /// </summary>
        private DatabaseProvider GetDatabaseProvider()
        {
            var providerString = _configuration["DatabaseProvider"]
                ?? _configuration["DatabaseType"]
                ?? "SQLiteInMemory";

            if (Enum.TryParse<DatabaseProvider>(providerString, out var provider))
            {
                return provider;
            }

            return DatabaseProvider.SQLiteInMemory;
        }

        /// <summary>
        /// Gets the connection string based on the configured database provider
        /// </summary>
        private string GetConnectionString()
        {
            return _databaseProvider switch
            {
                DatabaseProvider.SQLiteInMemory => "DataSource=file::memory:?cache=shared",
                DatabaseProvider.SQLiteFile => _configuration.GetConnectionString("TestDb")
                    ?? "DataSource=test.db",
                DatabaseProvider.LocalDb => _configuration.GetConnectionString("TestDb")
                    ?? @"Server=(localdb)\mssqllocaldb;Database=TestDB;Trusted_Connection=True",
                DatabaseProvider.SQLServer => _configuration.GetConnectionString("TestDb")
                    ?? @"Server=(localdb)\mssqllocaldb;Database=TestDB;Trusted_Connection=True",
                _ => "DataSource=file::memory:?cache=shared"
            };
        }

        /// <summary>
        /// Initializes the database schema and ensures it's ready for testing
        /// For SQLite in-memory, keeps the connection open to maintain the database
        /// </summary>
        private void InitializeDatabase()
        {
            lock (_lock)
            {
                if (!_databaseInitialized)
                {
                    using (TestDbContext context = CreateContext())
                    {
                        context.Database.EnsureDeleted();
                        context.Database.EnsureCreated();
                    }

                    _databaseInitialized = true;
                }
            }
        }

        /// <summary>
        /// Creates a new TestDbContext with the configured database provider
        /// For SQLite in-memory, reuses the shared connection to maintain the database
        /// </summary>
        public TestDbContext CreateContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

            switch (_databaseProvider)
            {
                case DatabaseProvider.SQLiteInMemory:
                    optionsBuilder.UseSqlite(_connectionString);
                    // Keep the connection open for in-memory SQLite
                    if (_sharedConnection == null)
                    {
                        var tempContext = new TestDbContext(optionsBuilder.Options);
                        _sharedConnection = tempContext.Database.GetDbConnection();
                        _sharedConnection.Open();
                    }
                    break;

                case DatabaseProvider.SQLiteFile:
                    optionsBuilder.UseSqlite(_connectionString);
                    break;

                case DatabaseProvider.LocalDb:
                case DatabaseProvider.SQLServer:
                    optionsBuilder.UseSqlServer(_connectionString);
                    break;
            }

            optionsBuilder.EnableSensitiveDataLogging();
            return new TestDbContext(optionsBuilder.Options);
        }
    }

    /// <summary>
    /// Enumeration of supported database providers for testing
    /// </summary>
    public enum DatabaseProvider
    {
        /// <summary>
        /// SQLite in-memory database (default, no external dependencies)
        /// </summary>
        SQLiteInMemory = 0,

        /// <summary>
        /// SQLite file-based database
        /// </summary>
        SQLiteFile = 1,

        /// <summary>
        /// SQL Server LocalDB (local development)
        /// </summary>
        LocalDb = 2,

        /// <summary>
        /// SQL Server (remote or local)
        /// </summary>
        SQLServer = 3
    }
}
