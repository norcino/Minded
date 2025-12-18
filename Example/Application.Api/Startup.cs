using System;
using System.IO;
using System.Reflection;
using Common.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.WebApi;
using Minded.Framework.Mediator;
using Minded.Extensions.Configuration;
using Minded.Extensions.Exception.Decorator;
using Minded.Extensions.Exception.Configuration;
using Minded.Extensions.Logging.Decorator;
using Minded.Extensions.Logging.Configuration;
using Minded.Extensions.Validation.Decorator;
using Minded.Extensions.Caching.Memory.Decorator;
using Minded.Extensions.Retry.Decorator;
using Minded.Extensions.Retry.Configuration;
using Minded.Extensions.DataProtection;
using Minded.Extensions.DataProtection.Abstractions;
using Minded.Framework.CQRS.Abstractions;
using Serilog;
using Serilog.Core;
using Application.Api.OData;
using Application.Api.Services;
using Application.Api.Hubs;
using Application.Api.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OData;

namespace Application.Api
{
    public class Startup
    {
        public static readonly ILoggerFactory AppLoggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
        public static readonly LoggingLevelSwitch LoggingLevelSwitch = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Information);
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment HostingEnvironment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            HostingEnvironment = env;

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddConfiguration(configuration)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LoggingLevelSwitch)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .WriteTo.File("log-.log", rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger();
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            // Reconfigure Serilog to add SignalR sink now that services are available
            var hubContext = app.ApplicationServices.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<LogHub>>();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LoggingLevelSwitch) // Use the level switch for runtime control
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning) // Filter out Microsoft logs below Warning
                .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning) // Filter out ASP.NET Core logs below Warning
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Error) // Only show EF Core errors
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Error) // Disable SQL query logging
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", Serilog.Events.LogEventLevel.Error) // Disable EF infrastructure logs
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning) // Filter out System logs below Warning
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .WriteTo.File("log-.log", rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .WriteTo.SignalR(hubContext) // Add SignalR sink for real-time log streaming
                .CreateLogger();

            if (HostingEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // Enable Swagger UI in development
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Minded Example API v1");
                    c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
                });

                // Seed the database with sample data for debugging
                SeedDatabaseForDevelopment(app);
            }

            app.UseCors(builder => builder
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(origin => true) // Allow any origin
                .AllowCredentials()); // Required for SignalR

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<LogHub>("/hubs/logs"); // SignalR hub endpoint
            });
        }

        /// <summary>
        /// Seeds the database with sample data for development and debugging purposes.
        /// This method is only called in development environment.
        /// </summary>
        /// <param name="app">The application builder</param>
        private void SeedDatabaseForDevelopment(IApplicationBuilder app)
        {
            using (IServiceScope scope = app.ApplicationServices.CreateScope())
            {
                IMindedExampleContext context = scope.ServiceProvider.GetService<IMindedExampleContext>();
                if (context != null)
                {
                    var seeder = new DatabaseSeeder(context);
                    seeder.Seed();
                }
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddLogging(logging =>
            {
                logging.AddConfiguration(Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
            });

            // Entity Framework context registration
            RegisterContext(services, HostingEnvironment);

            services.AddMemoryCache();

            // Register SignalR for real-time log streaming
            services.AddSignalR();

            // Register RuntimeConfigurationStore as singleton for runtime configuration management
            services.AddSingleton<RuntimeConfigurationStore>();

            services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), b =>
            {
                b.AddMediator();
                b.AddRestMediator();

                // Configure DataProtection with static defaults
                b.AddDataProtection(options =>
                {
                    options.ShowSensitiveData = false;
                });

                // Register custom logging sanitizer (optional)
                // This demonstrates how to add custom sanitization logic to the logging pipeline
                // The sanitizer will automatically be discovered and applied by the pipeline
                b.ServiceCollection.AddSingleton<Minded.Framework.CQRS.Abstractions.Sanitization.ILoggingSanitizer,
                    CustomLoggingSanitizer>();

                b.AddCommandValidationDecorator()
                .AddCommandExceptionDecorator(options =>
                {
                    options.Serialize = true;
                })
                .AddCommandRetryDecorator(options =>
                {
                    options.DefaultRetryCount = 3;
                    options.DefaultDelay1 = 100;
                    options.DefaultDelay2 = 200;
                    options.DefaultDelay3 = 400;
                    options.DefaultDelay2 = 700;
                    options.DefaultDelay3 = 1000;
                })
                .AddCommandLoggingDecorator(options =>
                {
                    options.Enabled = false;
                    options.LogMessageTemplateData = false;
                    options.LogOutcomeEntries = false;
                })
                .AddCommandHandlers();

                b.AddQueryValidationDecorator()
                .AddQueryExceptionDecorator(options =>
                {
                    options.Serialize = true;
                })
                .AddQueryRetryDecorator(applyToAllQueries: false, configureOptions: options =>
                {
                    options.DefaultRetryCount = 2;
                    options.DefaultDelay1 = 50;
                    options.ApplyToAllQueries = false;
                })
                .AddQueryLoggingDecorator(options =>
                {
                    options.Enabled = false;
                    options.LogMessageTemplateData = false;
                    options.LogOutcomeEntries = false;
                })
                .AddQueryMemoryCacheDecorator()
                .AddQueryHandlers();
            });

            // Configure runtime configuration providers using PostConfigure with dependency injection
            // This ensures we get the correct singleton instance of RuntimeConfigurationStore
            // from the final service provider, not a separate instance from BuildServiceProvider()
            services.AddOptions<DataProtectionOptions>()
                .PostConfigure<RuntimeConfigurationStore>((options, configStore) =>
                {
                    options.ShowSensitiveDataProvider = () => configStore.GetValue<bool>("DataProtection.ShowSensitiveData", false);
                });

            services.AddOptions<ExceptionOptions>()
                .PostConfigure<RuntimeConfigurationStore>((options, configStore) =>
                {
                    options.SerializeProvider = () => configStore.GetValue<bool>("Exception.Serialize", true);
                });

            services.AddOptions<RetryOptions>()
                .PostConfigure<RuntimeConfigurationStore>((options, configStore) =>
                {
                    options.DefaultRetryCountProvider = () => configStore.GetValue<int>("Retry.DefaultRetryCount", 3);
                    options.DefaultDelay1Provider = () => configStore.GetValue<int>("Retry.DefaultDelay1", 100);
                    options.DefaultDelay2Provider = () => configStore.GetValue<int>("Retry.DefaultDelay2", 200);
                    options.DefaultDelay3Provider = () => configStore.GetValue<int>("Retry.DefaultDelay3", 400);
                    options.DefaultDelay4Provider = () => configStore.GetValue<int>("Retry.DefaultDelay4", 700);
                    options.DefaultDelay5Provider = () => configStore.GetValue<int>("Retry.DefaultDelay5", 1000);
                    options.ApplyToAllQueriesProvider = () => configStore.GetValue<bool>("Retry.ApplyToAllQueries", false);
                });

            services.AddOptions<LoggingOptions>()
                .PostConfigure<RuntimeConfigurationStore>((options, configStore) =>
                {
                    options.EnabledProvider = () => configStore.GetValue<bool>("Logging.Enabled", false);
                    options.LogMessageTemplateDataProvider = () => configStore.GetValue<bool>("Logging.LogMessageTemplateData", false);
                    options.LogOutcomeEntriesProvider = () => configStore.GetValue<bool>("Logging.LogOutcomeEntries", false);
                    options.MinimumOutcomeSeverityLevelProvider = () =>
                    {
                        var level = configStore.GetValue<string>("Logging.MinimumOutcomeSeverityLevel", "Info");
                        return Enum.TryParse<Severity>(level, out var result) ? result : Severity.Info;
                    };
                });

            // Configure MVC with OData navigation property serialization
            // This ensures navigation properties are only serialized when explicitly requested via $expand
            // OData 9.x: Using attribute-based routing, so no EDM model or route components needed
            services.AddControllers()
                .AddODataNavigationPropertySerialization()
                .AddOData(options => options
                    .Select()
                    .Filter()
                    .OrderBy()
                    .Expand()
                    .Count()
                    .SetMaxTop(100));

            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

            // Configure Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Minded Example API",
                    Version = "v1",
                    Description = "Example API demonstrating the Minded framework with CQRS, Mediator pattern, and decorator-based cross-cutting concerns",
                    Contact = new OpenApiContact
                    {
                        Name = "Minded Framework",
                        Url = new Uri("https://github.com/norcino/Minded")
                    }
                });

                // Include XML comments for better API documentation
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                // Add support for CancellationToken in Swagger UI
                c.OperationFilter<SwaggerCancellationTokenOperationFilter>();

                // Add support for ODataQueryOptions in Swagger UI
                c.OperationFilter<SwaggerODataOperationFilter>();
            });
        }

        private static void RegisterContext(IServiceCollection services, IWebHostEnvironment env)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(Common.Configuration.Constants.ConfigConnectionStringName);
            DatabaseType databaseType = DatabaseType.SQLServer;

            try
            {
                databaseType = configuration?.GetValue<DatabaseType>("DatabaseType") ?? DatabaseType.SQLServer;
            }
            catch
            {
                //LoggerFactory.CreateLogger(typeof(DependencyInjectionConfiguration))?.
                //    LogWarning("Missing or invalid configuration: DatabaseType");
                databaseType = DatabaseType.SQLServer;
            }

            if (env != null && env.IsProduction())
            {
                if (databaseType == DatabaseType.SQLiteInMemory || databaseType == DatabaseType.LocalDb)
                {
                    throw new NotSupportedException($"Cannot use database type {databaseType} for production environment");
                }
            }

            switch (databaseType)
            {
                case DatabaseType.SQLiteInMemory:
                    // Use SQLite in memory database for testing
                    services.AddDbContext<MindedExampleContext>(options =>
                    {
                        options.UseSqlite($"DataSource='file::memory:?cache=shared'");
                    });

                    // Use singleton context when using SQLite in memory if the connection is closed the database is going to be destroyed
                    // so must use a singleton context, open the connection and manually close it when disposing the context
                    services.AddSingleton<IMindedExampleContext>(s =>
                    {
                        MindedExampleContext context = s.GetService<MindedExampleContext>();
                        context.Database.OpenConnection();
                        context.Database.EnsureCreated();

                        // Seed the database with sample data for debugging
                        var seeder = new DatabaseSeeder(context);
                        seeder.Seed();

                        return context;
                    });
                    break;
                case DatabaseType.LocalDb:
                    // Local DB does support creation of the database
                    services.AddDbContext<MindedExampleContext>(options =>
                    {
                        options.UseSqlServer(connectionString);
                        options.UseLoggerFactory(AppLoggerFactory);
                    });

                    services.AddTransient<IMindedExampleContext>(service =>
                    {
                        MindedExampleContext context = services.BuildServiceProvider()
                        .GetService<MindedExampleContext>();
                        context.Database.EnsureCreated();

                        // Seed the database with sample data for debugging (only in development)
                        if (env != null && env.IsDevelopment())
                        {
                            var seeder = new DatabaseSeeder(context);
                            seeder.Seed();
                        }

                        return context;
                    });
                    break;
                case DatabaseType.SQLServer:
                default:
                    // Use SQL Server testing configuration
                    if (!(env != null && env.IsProduction()))
                    {
                        services.AddDbContext<MindedExampleContext>(options =>
                        {
                            options.UseSqlServer(connectionString);
                         //   options.UseLoggerFactory(AppLoggerFactory);
                        });

                        services.AddTransient<IMindedExampleContext>(s =>
                        {
                            MindedExampleContext context = s.GetService<MindedExampleContext>();
                            context.Database.EnsureCreated();

                            // Seed the database with sample data for debugging (only in development)
                            if (env != null && env.IsDevelopment())
                            {
                                var seeder = new DatabaseSeeder(context);
                                seeder.Seed();
                            }

                            return context;
                        });

                        break;
                    }

                    // Use SQL Server production configuration
                    services.AddDbContextPool<MindedExampleContext>(options =>
                    {
                        // Production setup using SQL Server
                        options.UseSqlServer(connectionString);
                    }, poolSize: 5);

                    services.AddTransient<IMindedExampleContext>(service =>
                        services.BuildServiceProvider()
                        .GetService<MindedExampleContext>());
                    break;
            }
        }
    }
}
