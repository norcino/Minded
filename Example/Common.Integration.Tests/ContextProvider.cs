using Application.Api;
using Common.Configuration;
using Data.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

//namespace Common.IntegrationTests
//{
    /// <summary>
    /// This class generates the Context used by persisters and tests.
    /// This class must share the same ServiceProvider used in the Integration Tests
    /// in order to share the same Context. If not the database can result locked when using
    /// Idempotent Integration Tests
    /// </summary>
    //public static class ContextProvider
    //{
        //private static bool _requiresDbDeletion;

        //private static IConfiguration _applicationConfiguration;
        //public static IConfiguration ApplicationConfiguration
        //{
        //    get
        //    {
        //        if (_applicationConfiguration != null) return _applicationConfiguration;

        //        _applicationConfiguration = new ConfigurationBuilder()
        //            .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
        //            .AddEnvironmentVariables()
        //            .Build();
                
        //        return _applicationConfiguration;
        //    }
        //}
        //private static ServiceProvider _serviceProvider;
        //public static ServiceProvider ServiceProvider
        //{
        //    get
        //    {
        //        if (_serviceProvider != null) return _serviceProvider;

        //        var serviceCollection = new ServiceCollection();
        //        serviceCollection.AddSingleton<IConfiguration>(ApplicationConfiguration);
        //        var databaseType = ApplicationConfiguration?.GetValue<DatabaseType>("DatabaseType") ?? DatabaseType.SQLServer;                
        //        _requiresDbDeletion = databaseType == DatabaseType.SQLServer;
                
        //        Startup.RegisterContext(serviceCollection, null);

        //        _serviceProvider = serviceCollection.BuildServiceProvider();
        //        return _serviceProvider;
        //    }
        //    set
        //    {
        //        _serviceProvider = value;
        //    }
        //}

        /// <summary>
        /// Generate the db context
        /// </summary>
        /// <returns>DB Context</returns>
        //public static IMindedExampleContext GetContext()
        //{            
        //    return ServiceProvider.GetService<IMindedExampleContext>();
        //}

        //public static void Dispose()
        //{
        //    ServiceProvider?.Dispose();
        //    ServiceProvider = null;
        //}

        //public static void ResetDatabase()
        //{
        //    if (_requiresDbDeletion)
        //    {
        //        GetContext()?.Database?.EnsureDeleted();
        //        GetContext()?.Database?.EnsureCreated();
        //    }
        //}
//    }
//}