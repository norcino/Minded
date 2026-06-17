using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MindedExample.Domain;
using MindedExample.Tests.Common;
using MindedExample.Tests.E2E.Common;
using QM.Common.Testing;

namespace MindedExample.Api.E2ETests
{
    /// <summary>
    /// E2E tests for runtime configuration management (ConfigurationsController).
    /// Reads require authentication; updates require the CanUpdateConfiguration permission
    /// (held by tenant admins). Runs against the real JWT pipeline.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ConfigurationsE2ETests : BaseE2ETest
    {
        protected override bool UseTestAuthentication => false;

        [TestMethod]
        public async Task GET_configurations_should_return_entries_for_authenticated_user()
        {
            var owner = await RegisterTenantOwnerAsync();
            UseBearer(owner.AccessToken);

            var response = await _sutClient.GetAsync("/api/configurations");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var entries = await response.Content.ReadAsAsync<List<ConfigurationEntry>>();
            entries.Should().NotBeEmpty();
            entries.Should().OnlyContain(e => !string.IsNullOrWhiteSpace(e.Key));
        }

        [TestMethod]
        public async Task GET_configurations_by_key_should_return_the_entry()
        {
            var owner = await RegisterTenantOwnerAsync();
            UseBearer(owner.AccessToken);
            var anyKey = (await GetConfigurationsAsync()).First().Key;

            var response = await _sutClient.GetAsync($"/api/configurations/{anyKey}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var entry = await response.Content.ReadAsAsync<ConfigurationEntry>();
            entry.Key.Should().Be(anyKey);
        }

        [TestMethod]
        public async Task GET_configurations_by_unknown_key_should_return_404NotFound()
        {
            var owner = await RegisterTenantOwnerAsync();
            UseBearer(owner.AccessToken);

            var response = await _sutClient.GetAsync("/api/configurations/Unknown.Key.Xyz");

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task PUT_configurations_should_update_value_and_persist_for_subsequent_reads()
        {
            var owner = await RegisterTenantOwnerAsync();
            UseBearer(owner.AccessToken);

            var booleanEntry = (await GetConfigurationsAsync())
                .First(e => e.Type != null && e.Type.ToLowerInvariant().Contains("bool"));
            var newValue = !System.Convert.ToBoolean(booleanEntry.Value?.ToString());

            var update = await _sutClient.PutAsync($"/api/configurations/{booleanEntry.Key}",
                new UpdateConfigurationRequest { Value = newValue });
            update.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var reread = await _sutClient.GetAsync($"/api/configurations/{booleanEntry.Key}");
            reread.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var entry = await reread.Content.ReadAsAsync<ConfigurationEntry>();
            System.Convert.ToBoolean(entry.Value?.ToString()).Should().Be(newValue);
        }

        [TestMethod]
        public async Task PUT_configurations_should_return_403Forbidden_for_member_without_permission()
        {
            var owner = await RegisterTenantOwnerAsync();
            var member = await RegisterInvitedMemberAsync(owner.AccessToken);
            UseBearer(owner.AccessToken);
            var anyKey = (await GetConfigurationsAsync()).First().Key;

            UseBearer(member.AccessToken);
            var response = await _sutClient.PutAsync($"/api/configurations/{anyKey}",
                new UpdateConfigurationRequest { Value = false });

            response.Should().HaveHttpStatusCode(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task GET_configurations_should_return_401Unauthorized_for_anonymous_callers()
        {
            UseAnonymous();
            var response = await _sutClient.GetAsync("/api/configurations");

            response.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task PUT_configurations_should_return_401Unauthorized_for_anonymous_callers()
        {
            UseAnonymous();
            var response = await _sutClient.PutAsync("/api/configurations/Logging.Enabled",
                new UpdateConfigurationRequest { Value = false });

            response.Should().HaveHttpStatusCode(HttpStatusCode.Unauthorized);
        }

        private async Task<List<ConfigurationEntry>> GetConfigurationsAsync()
        {
            var response = await _sutClient.GetAsync("/api/configurations");
            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            return await response.Content.ReadAsAsync<List<ConfigurationEntry>>();
        }
    }

    /// <summary>
    /// E2E tests for the health check endpoint. Deliberately anonymous: health probes
    /// must work without credentials.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class HealthCheckE2ETests : BaseE2ETest
    {
        protected override bool UseTestAuthentication => false;

        [TestMethod]
        public async Task GET_healthcheck_should_report_environment_version_and_active_database()
        {
            UseAnonymous();
            var response = await _sutClient.GetAsync("/api/healthcheck");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var health = await response.Content.ReadAsAsync<HealthCheckResponse>();

            health.Environemnt.Should().Be(CurrentTestingProfile);
            health.Version.Should().NotBeNullOrWhiteSpace();

            if (CurrentTestingProfile == "E2ELive")
            {
                health.Database.Should().Be("SQLiteInMemory");
            }
            else if (PostgreSqlTestDatabase.RunDatabaseName != null)
            {
                health.Database.Should().Be(PostgreSqlTestDatabase.RunDatabaseName);
            }
            else
            {
                health.Database.Should().NotBeNullOrWhiteSpace();
            }
        }

        private class HealthCheckResponse
        {
            public string Environemnt { get; set; }
            public string Version { get; set; }
            public string Database { get; set; }
        }
    }
}
