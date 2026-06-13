using System;
using System.Reflection;
using MindedExample.Infrastructure.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Minded.Framework.Mediator;

namespace MindedExample.Api.Controllers
{
    [Route("api/[controller]")]
    public class HealthCheckController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConfiguration _configuration;

        public HealthCheckController(IMediator mediator, IWebHostEnvironment hostingEnvironment, IConfiguration configuration)
        {
            _mediator = mediator;
            _hostingEnvironment = hostingEnvironment;
            _configuration = configuration;
        }

        [HttpGet]
        public ActionResult Get()
        {
            dynamic result = new
            {
                Environemnt = _hostingEnvironment.EnvironmentName,
                Version = typeof(Startup).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,
                Database = GetActiveDatabaseName()
            };

            return new OkObjectResult(result);
        }

        /// <summary>
        /// Resolves the database name for the active DatabaseType so the healthcheck reports
        /// the database the application is actually configured against.
        /// </summary>
        private string GetActiveDatabaseName()
        {
            DatabaseType databaseType;
            try
            {
                databaseType = _configuration.GetValue<DatabaseType>("DatabaseType");
            }
            catch
            {
                databaseType = DatabaseType.SQLServer;
            }

            if (databaseType == DatabaseType.SQLiteInMemory)
            {
                return nameof(DatabaseType.SQLiteInMemory);
            }

            var connectionString = databaseType == DatabaseType.PostgreSQL
                ? _configuration.GetConnectionString(Constants.ConfigPostgreSqlConnectionStringName)
                    ?? _configuration.GetConnectionString(Constants.ConfigConnectionStringName)
                : _configuration.GetConnectionString(Constants.ConfigConnectionStringName);

            if (connectionString == null)
            {
                return null;
            }

            foreach (var token in connectionString.Split(';'))
            {
                if (token.TrimStart().StartsWith("database", StringComparison.InvariantCultureIgnoreCase))
                {
                    return token.Replace("database", "", StringComparison.InvariantCultureIgnoreCase)
                        .Replace("=", "")
                        .Trim(' ');
                }
            }

            return connectionString;
        }
    }
}
