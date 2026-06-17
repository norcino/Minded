using MindedExample.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Moq;
using System.Threading.Tasks;
using MindedExample.Domain;
using MindedExample.Tests.Common;
using MindedExample.Infrastructure.Configuration;
using MindedExample.Api;
using System.Data.Common;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using QM.Common.Testing;
using System.Linq.Expressions;
using System.Linq;
using System.Threading;

namespace MindedExample.Tests.E2E.Common
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

        /// <summary>
        /// Id of the baseline user every request is authenticated as.
        /// This user exists in the database (created during test initialization),
        /// so user-listing endpoints always return at least this record.
        /// </summary>
        protected static int AuthenticatedUserId => TestAuthenticationState.UserId;

        /// <summary>Tenant of the baseline authenticated user.</summary>
        protected static int? AuthenticatedTenantId => TestAuthenticationState.TenantId;

        /// <summary>
        /// When true (default), every request is authenticated as the baseline user via
        /// <see cref="TestAuthenticationHandler"/> and Minded authorization is granted by
        /// <see cref="TestAdminAuthorizationContextAccessor"/>. Override with false to exercise
        /// the real JWT bearer pipeline (anonymous requests, real tokens) — used by auth tests.
        /// </summary>
        protected virtual bool UseTestAuthentication => true;

        /// <summary>
        /// Direct access to the test database for arranging or asserting state that has no
        /// API surface (e.g. password-reset tokens, which are normally delivered by email).
        /// </summary>
        protected IMindedExampleContext Context => _context;

        /// <summary>Name of the active testing profile (E2ELive, E2E, UnitTesting).</summary>
        protected static string CurrentTestingProfile => s_currentTestingProfile.ToString();

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
            _sutClient = CreateTestApplication();
        }

        [TestInitialize]
        public async Task BaseTestTestInitialize()
        {
            await ResetDb();
            await SeedAuthorizationData();
        }

        /// <summary>
        /// Seeds the authorization data (role-permissions) required for E2E tests.
        /// Uses raw SQL to insert into RolePermissions and UserRoles join tables.
        /// </summary>
        private async Task SeedAuthorizationData()
        {
            if (s_currentTestingProfile == TestingProfile.UnitTesting)
                return;

            if (_context is MindedExampleContext concreteContext)
            {
                // RolePermissions rows are tenant-scoped (PK and FK include TenantId),
                // so a tenant must exist to own the seeded mappings.
                var tenant = concreteContext.Tenants.FirstOrDefault();
                if (tenant == null)
                {
                    tenant = new Tenant { Name = "E2E Tenant" };
                    concreteContext.Tenants.Add(tenant);
                    await concreteContext.SaveChangesAsync();
                }

                // Baseline user impersonated by TestAuthenticationHandler. Controllers such as
                // UsersController verify the caller's user row exists in the current tenant,
                // so the principal must be backed by a real record.
                var baselineUser = new User
                {
                    Name = "E2E",
                    Surname = "Admin",
                    Email = TestAuthenticationState.Email,
                    PasswordHash = "e2e-test-only",
                    TenantId = tenant.Id,
                    TenantRole = TenantMemberRoles.Owner,
                    IsActive = true,
                    IsGlobalAdmin = false
                };
                concreteContext.Users.Add(baselineUser);
                await concreteContext.SaveChangesAsync();

                TestAuthenticationState.UserId = baselineUser.Id;
                TestAuthenticationState.TenantId = tenant.Id;
                TestAuthenticationState.TenantRole = baselineUser.TenantRole;
                TestAuthenticationState.IsGlobalAdmin = false;

                // Seed all role-permission mappings for Admin role and grant it to the baseline
                // user. Inserted through the shared-type entity sets (not raw SQL) so EF
                // generates correctly quoted, schema-qualified SQL for every database provider.
                var rolePermissions = concreteContext.Set<Dictionary<string, object>>("RolePermissions");
                foreach (var permission in DefaultRolesDefinition.AllPermissions)
                {
                    rolePermissions.Add(new Dictionary<string, object>
                    {
                        ["TenantId"] = tenant.Id,
                        ["RoleName"] = Roles.Admin,
                        ["PermissionName"] = permission
                    });
                }

                concreteContext.Set<Dictionary<string, object>>("UserRoles").Add(new Dictionary<string, object>
                {
                    ["TenantId"] = tenant.Id,
                    ["UserId"] = baselineUser.Id,
                    ["RoleName"] = Roles.Admin
                });

                await concreteContext.SaveChangesAsync();
                concreteContext.ChangeTracker.Clear();
            }
        }

        /// <summary>
        /// This method is used to Seed data consumed by the tested application and components.
        /// </summary>
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

        protected HttpClient CreateTestApplication(Action<IServiceCollection> serviceCollectionSetup = null, Dictionary<string, string> configurationOverride = null)
        {
            _configuration = BuildTestConfiguration(configurationOverride);

            s_currentTestingProfile = (TestingProfile) Enum.Parse(typeof(TestingProfile), _configuration.GetValue<string>("TestingProfile"));

            // The PostgreSQL E2E profile works against a unique per-run database so runs are
            // isolated and re-runnable; redirect the connection string before the host starts.
            if (s_currentTestingProfile == TestingProfile.E2E && GetConfiguredDatabaseType() == DatabaseType.PostgreSQL)
            {
                var baseConnectionString =
                    _configuration.GetConnectionString(Constants.ConfigPostgreSqlConnectionStringName)
                    ?? _configuration.GetConnectionString(Constants.ConfigConnectionStringName);
                var runConnectionString = PostgreSqlTestDatabase.GetRunConnectionString(baseConnectionString);

                var overrides = configurationOverride != null
                    ? new Dictionary<string, string>(configurationOverride)
                    : new Dictionary<string, string>();
                overrides[$"ConnectionStrings:{Constants.ConfigPostgreSqlConnectionStringName}"] = runConnectionString;
                _configuration = BuildTestConfiguration(overrides);
            }

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

                    if (UseTestAuthentication)
                    {
                        // Replace JWT bearer authentication with a scheme that authenticates every
                        // request as the baseline test user (see TestAuthenticationHandler).
                        services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                            options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                        }).AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            TestAuthenticationHandler.SchemeName, _ => { });

                        services.AddScoped<Minded.Extensions.Authorization.IAuthorizationContextAccessor>(sp =>
                            new TestAdminAuthorizationContextAccessor());
                    }

                    serviceCollectionSetup?.Invoke(services);

                    _serviceProvider = services.BuildServiceProvider();
                });

                builder.UseConfiguration(_configuration);
                builder.UseEnvironment(s_currentTestingProfile.ToString());
            });

            HttpClient client = applicationFactory.CreateClient();
            _context = _serviceProvider.GetService<IMindedExampleContext>();

            return client;
        }

        private void SetupDbContextMockObject()
        {
            _mockIMindedExampleContext.Setup(c => c.Dispose());
            _mockIMindedExampleContext.Setup<Task<int>>(c => c.SaveChangesAsync()).ReturnsAsync(1);

            _mockIMindedExampleContext.SetupGet(c => c.Categories).Returns(new List<Category>().GetMockDbSet().Object);
            _mockIMindedExampleContext.SetupGet(t => t.Transactions).Returns(new List<Transaction>().GetMockDbSet().Object);
            _mockIMindedExampleContext.SetupGet(t => t.Users).Returns(new List<User>().GetMockDbSet().Object);
        }

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

                    services.AddDbContext<MindedExampleContext>(options =>
                    {
                        options.UseSqlite($"DataSource='file::memory:?cache=shared'");
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                        // SQLite does not support System.Transactions ambient transactions;
                        // commands decorated with [TransactionalCommand] would otherwise fail under E2ELive.
                        options.ConfigureWarnings(w => w.Ignore(RelationalEventId.AmbientTransactionWarning));
                    }, ServiceLifetime.Singleton);

                    services.AddSingleton<IMindedExampleContext>(s =>
                    {
                        MindedExampleContext context = s.GetService<MindedExampleContext>();
                        _connection = context.Database.GetDbConnection();
                        _connection.Open();
                        context.Database.EnsureCreated();

                        if (_context == null)
                        {
                            _context = context;
                            _seeder = new Seeder(s_currentTestingProfile, _context, _mockIMindedExampleContext);
                        }

                        return context;
                    });
                    break;
                case TestingProfile.E2E:
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

                    // Remove the options registered by Startup as well: AddDbContext uses TryAdd
                    // internally, so without this the provider configured below would be ignored.
                    ServiceDescriptor descriptorOptionsE2E = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MindedExampleContext>));
                    if (descriptorOptionsE2E != null)
                    {
                        services.Remove(descriptorOptionsE2E);
                    }

                    DatabaseType e2eDatabaseType = GetConfiguredDatabaseType();
                    services.AddDbContext<MindedExampleContext>(options =>
                    {
                        if (e2eDatabaseType == DatabaseType.PostgreSQL)
                        {
                            options.UseNpgsql(_configuration.GetConnectionString(Constants.ConfigPostgreSqlConnectionStringName));
                        }
                        else
                        {
                            options.UseSqlServer(_configuration.GetConnectionString(Constants.ConfigConnectionStringName));
                            options.UseLoggerFactory(Startup.AppLoggerFactory);
                        }
                    });

                    services.AddTransient<IMindedExampleContext>(s =>
                    {
                        MindedExampleContext context = s.GetService<MindedExampleContext>();
                        context.Database.EnsureCreated();

                        // Capture only the first (root-scoped) instance for test-side access:
                        // request-scoped instances are disposed when their request ends, and
                        // reassigning here would leave _context pointing at a disposed context.
                        if (_context == null)
                        {
                            _context = context;
                            _seeder = new Seeder(s_currentTestingProfile, _context, _mockIMindedExampleContext);
                        }

                        return context;
                    });
                    break;
            }
        }

        #region Real-authentication helpers (for suites with UseTestAuthentication = false)

        /// <summary>Default password used by the real-authentication test helpers.</summary>
        protected const string DefaultTestPassword = "Passw0rd!";

        /// <summary>Attaches a bearer token to every subsequent request of the test client.</summary>
        protected void UseBearer(string accessToken)
            => _sutClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        /// <summary>Removes any bearer token so subsequent requests are anonymous.</summary>
        protected void UseAnonymous()
            => _sutClient.DefaultRequestHeaders.Authorization = null;

        /// <summary>
        /// Registers a new tenant through the public API (create-tenant mode) and returns the
        /// authenticated owner. Leaves the client anonymous.
        /// </summary>
        protected async Task<MindedExample.Api.Models.AuthResponse> RegisterTenantOwnerAsync(
            string email = null, string password = DefaultTestPassword, string tenantName = null)
        {
            UseAnonymous();
            var response = await _sutClient.PostAsync("/api/auth/register", new MindedExample.Api.Models.RegisterRequest
            {
                Name = AnonymousData.Any.String(),
                Surname = AnonymousData.Any.String(),
                Email = email ?? AnonymousData.Any.Email(),
                Password = password,
                TenantName = tenantName ?? $"Tenant {AnonymousData.Any.String()}"
            });

            response.EnsureSuccessStatusCode();
            return await MindedExample.Tests.Common.HttpContentExtensions.ReadAsAsync<MindedExample.Api.Models.AuthResponse>(response.Content);
        }

        /// <summary>Creates a tenant invite as the given owner and returns it. Leaves the bearer set to the owner.</summary>
        protected async Task<MindedExample.Api.Models.TenantInviteDto> CreateInviteAsync(string ownerAccessToken, string inviteeEmail)
        {
            UseBearer(ownerAccessToken);
            var response = await _sutClient.PostAsync("/api/tenant-admin/invites",
                new MindedExample.Api.Models.CreateInviteRequest { Email = inviteeEmail });

            response.EnsureSuccessStatusCode();
            return await MindedExample.Tests.Common.HttpContentExtensions.ReadAsAsync<MindedExample.Api.Models.TenantInviteDto>(response.Content);
        }

        /// <summary>
        /// Invites and registers a member into the owner's tenant through the public API
        /// (invite + accept-invite) and returns the authenticated member. Leaves the client anonymous.
        /// </summary>
        protected async Task<MindedExample.Api.Models.AuthResponse> RegisterInvitedMemberAsync(
            string ownerAccessToken, string memberEmail = null, string password = DefaultTestPassword)
        {
            memberEmail ??= AnonymousData.Any.Email();
            var invite = await CreateInviteAsync(ownerAccessToken, memberEmail);

            UseAnonymous();
            var accept = await _sutClient.PostAsync("/api/auth/accept-invite", new MindedExample.Api.Models.AcceptInviteRequest
            {
                CodeOrToken = invite.Token,
                Email = memberEmail,
                Name = AnonymousData.Any.String(),
                Surname = AnonymousData.Any.String(),
                Password = password
            });

            accept.EnsureSuccessStatusCode();
            return await MindedExample.Tests.Common.HttpContentExtensions.ReadAsAsync<MindedExample.Api.Models.AuthResponse>(accept.Content);
        }

        /// <summary>
        /// Creates a global administrator directly in the database (there is no public API for
        /// this: global admins are provisioned out-of-band) and logs in through the API,
        /// returning the authenticated admin. Leaves the client anonymous.
        /// </summary>
        protected async Task<MindedExample.Api.Models.AuthResponse> CreateGlobalAdminAsync(
            string email = null, string password = DefaultTestPassword)
        {
            email ??= AnonymousData.Any.Email().ToLowerInvariant();

            var admin = new User
            {
                Name = "Global",
                Surname = "Admin",
                Email = email,
                TenantId = null,
                TenantRole = TenantMemberRoles.Member,
                IsActive = true,
                IsGlobalAdmin = true
            };
            admin.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<User>().HashPassword(admin, password);

            if (_context is MindedExampleContext concreteContext)
            {
                concreteContext.Users.Add(admin);
                await concreteContext.SaveChangesAsync();
                concreteContext.ChangeTracker.Clear();
            }

            UseAnonymous();
            var login = await _sutClient.PostAsync("/api/auth/login",
                new MindedExample.Api.Models.LoginRequest { Email = email, Password = password });
            login.EnsureSuccessStatusCode();
            return await MindedExample.Tests.Common.HttpContentExtensions.ReadAsAsync<MindedExample.Api.Models.AuthResponse>(login.Content);
        }

        #endregion

        private static IConfigurationRoot BuildTestConfiguration(Dictionary<string, string> configurationOverride)
        {
            // Precedence (last wins): testappsettings.json < MINDEDTEST_* environment
            // variables (profile selection without editing files) < per-test overrides.
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("testappsettings.json", optional: false)
                .AddEnvironmentVariables("MINDEDTEST_")
                .AddInMemoryCollection(configurationOverride)
                .Build();
        }

        private DatabaseType GetConfiguredDatabaseType()
        {
            try
            {
                return _configuration.GetValue<DatabaseType>("DatabaseType");
            }
            catch
            {
                return DatabaseType.SQLServer;
            }
        }

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
                if (_context is MindedExampleContext concreteContext)
                {
                    concreteContext.ChangeTracker.Clear();

                    string transactionsTable = concreteContext.Model.FindEntityType(typeof(Transaction))?.GetTableName() ?? "Transactions";
                    string categoriesTable = concreteContext.Model.FindEntityType(typeof(Category))?.GetTableName() ?? "Categories";
                    string usersTable = concreteContext.Model.FindEntityType(typeof(User))?.GetTableName() ?? "Users";
                    string passwordResetTokensTable = concreteContext.Model.FindEntityType(typeof(PasswordResetToken))?.GetTableName() ?? "PasswordResetTokens";
                    string tenantInvitesTable = concreteContext.Model.FindEntityType(typeof(TenantInvite))?.GetTableName() ?? "TenantInvites";
                    string tenantJoinRequestsTable = concreteContext.Model.FindEntityType(typeof(TenantJoinRequest))?.GetTableName() ?? "TenantJoinRequests";
                    string tenantsTable = concreteContext.Model.FindEntityType(typeof(Tenant))?.GetTableName() ?? "Tenants";

#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                    await concreteContext.Database.ExecuteSqlRawAsync($"DELETE FROM {transactionsTable}", cancellationToken);
                    await concreteContext.Database.ExecuteSqlRawAsync($"DELETE FROM {categoriesTable}", cancellationToken);
                    await concreteContext.Database.ExecuteSqlRawAsync("DELETE FROM UserRoles", cancellationToken);
                    await concreteContext.Database.ExecuteSqlRawAsync("DELETE FROM RolePermissions", cancellationToken);
                    // Auth-flow tables must go before Users: TenantInvites references users with a Restrict FK
                    await concreteContext.Database.ExecuteSqlRawAsync($"DELETE FROM {passwordResetTokensTable}", cancellationToken);
                    await concreteContext.Database.ExecuteSqlRawAsync($"DELETE FROM {tenantInvitesTable}", cancellationToken);
                    await concreteContext.Database.ExecuteSqlRawAsync($"DELETE FROM {tenantJoinRequestsTable}", cancellationToken);
                    // Tenants reference their legal owner with a Restrict FK: detach before deleting
                    // users, then remove the tenants themselves (recreated by SeedAuthorizationData)
                    await concreteContext.Database.ExecuteSqlRawAsync($"UPDATE {tenantsTable} SET LegalOwnerUserId = NULL", cancellationToken);
                    await concreteContext.Database.ExecuteSqlRawAsync($"DELETE FROM {usersTable}", cancellationToken);
                    await concreteContext.Database.ExecuteSqlRawAsync($"DELETE FROM {tenantsTable}", cancellationToken);
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.

                    await concreteContext.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence", cancellationToken);

                    concreteContext.ChangeTracker.Clear();
                }

                _seeder = new Seeder(s_currentTestingProfile, _context, _mockIMindedExampleContext);
                return;
            }

            await _context.Database.EnsureDeletedAsync(cancellationToken);
            await _context.Database.EnsureCreatedAsync(cancellationToken);

            _seeder = new Seeder(s_currentTestingProfile, _context, _mockIMindedExampleContext);
        }
    }

    /// <summary>
    /// Test-only authorization context accessor that always returns an admin context with all permissions.
    /// </summary>
    internal class TestAdminAuthorizationContextAccessor : Minded.Extensions.Authorization.IAuthorizationContextAccessor
    {
        private static readonly string[] _adminRoles = new[] { Roles.Admin };

        private static readonly string[] _adminPermissions = new[]
        {
            Permissions.CanCreateCategory, Permissions.CanCreateRootCategory,
            Permissions.CanUpdateCategory, Permissions.CanDeleteCategory,
            Permissions.CanCreateTransaction, Permissions.CanUpdateTransaction,
            Permissions.CanDeleteTransaction, Permissions.CanCreateUser,
            Permissions.CanUpdateUser, Permissions.CanDeleteUser,
            Permissions.CanManageRoles, Permissions.CanAssignRoles
        };

        // Built lazily so claims reflect the baseline user/tenant established by
        // SeedAuthorizationData during TestInitialize (mirrors HttpCurrentUserAccessor).
        public Minded.Extensions.Authorization.AuthorizationContext Current
        {
            get
            {
                var claims = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
                {
                    ["is_global_admin"] = TestAuthenticationState.IsGlobalAdmin.ToString().ToLowerInvariant()
                };

                if (TestAuthenticationState.TenantId.HasValue)
                {
                    claims["tenant_id"] = TestAuthenticationState.TenantId.Value.ToString();
                }

                if (TestAuthenticationState.UserId != 0)
                {
                    claims["sub"] = TestAuthenticationState.UserId.ToString();
                }

                return new Minded.Extensions.Authorization.AuthorizationContext(
                    true, _adminRoles, _adminPermissions, claims);
            }
        }
    }
}
