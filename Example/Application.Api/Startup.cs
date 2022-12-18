using System;
using Common.Configuration;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Minded.Framework.Mediator;
using Minded.Extensions.Configuration;
using Minded.Extensions.Exception.Decorator;
using Minded.Extensions.Logging.Decorator;
using Minded.Extensions.Validation.Decorator;
using System.Linq;
using System.Reflection;
using Minded.Framework.CQRS.Command;

namespace Application.Api
{
    public class Startup
    {
        public static readonly ILoggerFactory AppLoggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
        public IConfigurationRoot Configuration { get; }
        public IWebHostEnvironment HostingEnvironment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            HostingEnvironment = env;

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddConfiguration(configuration)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {            
            if (HostingEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin();
            });
            
            app.UseMvc(routeBuilder =>
            {
                routeBuilder
                    .Expand()
                    .Filter()
                    .OrderBy(QueryOptionSetting.Allowed)
                    .MaxTop(100)
                    .Count();
                routeBuilder.EnableDependencyInjection();
            });
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

            services.AddMinded(assembly => assembly.Name.StartsWith("Service."), b =>
            {
                b.AddMediator();

                b.AddCommandValidationDecorator()
                .AddCommandExceptionDecorator()
                .AddCommandLoggingDecorator()
                .RegisterCommandHandlers();

                b.AddQueryExceptionDecorator()
                .AddQueryLoggingDecorator()
                .AddQueryHandlers();
            });

            // Add framework services.
            services.AddOData();

            services.AddMvc(
                options => options.EnableEndpointRouting = false
            )
            .AddApplicationPart(typeof(Controllers.BaseController).Assembly)
            .AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);            
        }

        public static void RegisterContext(IServiceCollection services, IWebHostEnvironment env)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(Constants.ConfigConnectionStringName);
            var databaseType = DatabaseType.SQLServer;

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
                        var context = s.GetService<MindedExampleContext>();
                        context.Database.OpenConnection();
                        context.Database.EnsureCreated();
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
                        var context = services.BuildServiceProvider()
                        .GetService<MindedExampleContext>();
                        context.Database.EnsureCreated();
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

                        services.AddSingleton<IMindedExampleContext>(s =>
                        {
                            var context = s.GetService<MindedExampleContext>();
                            context.Database.EnsureCreated();
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
