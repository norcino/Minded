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
using Minded.Extensions.Logging.Decorator;
using Minded.Extensions.Validation.Decorator;
using Minded.Extensions.Caching.Memory.Decorator;
using Minded.Extensions.Retry.Decorator;
using Minded.Extensions.DataProtection;
using Serilog;
using Application.Api.OData;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OData;

namespace Application.Api
{
    public class Startup
    {
        public static readonly ILoggerFactory AppLoggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
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
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .WriteTo.File("log-.log", rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger();
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
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

            app.UseCors(builder => builder.AllowAnyOrigin());

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
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

            services.AddMinded(Configuration, assembly => assembly.Name.StartsWith("Service."), b =>
            {
                b.AddMediator();
                b.AddRestMediator();

                // Configure DataProtection to show sensitive data only in development
                // This protects PII and confidential data in logs for GDPR/CCPA compliance
                b.AddDataProtection(options =>
                {
                    options.ShowSensitiveDataProvider = () => HostingEnvironment.IsDevelopment();
                });

                b.AddCommandValidationDecorator()
                .AddCommandExceptionDecorator()
                .AddCommandRetryDecorator(options =>
                {
                    options.DefaultRetryCount = 3;
                    options.DefaultDelay1 = 100;
                    options.DefaultDelay2 = 200;
                    options.DefaultDelay3 = 400;
                })
                .AddCommandLoggingDecorator()
                .AddCommandHandlers();

                b.AddQueryValidationDecorator()
                .AddQueryExceptionDecorator()
                .AddQueryRetryDecorator(applyToAllQueries: false, configureOptions: options =>
                {
                    options.DefaultRetryCount = 2;
                    options.DefaultDelay1 = 50;
                })
                .AddQueryLoggingDecorator()
                .AddQueryMemoryCacheDecorator()
                .AddQueryHandlers();
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
            var connectionString = configuration.GetConnectionString(Constants.ConfigConnectionStringName);
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
