using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MindedExample.Tests.E2E.Common;

namespace MindedExample.Api.E2ETests
{
    /// <summary>
    /// Systematic authorization negative-path matrix. Endpoints are discovered by reflection
    /// over the API controllers, so the suite stays in sync automatically:
    /// - a newly added endpoint without [Authorize] fails the anonymous allow-list guard;
    /// - every protected endpoint is verified to reject anonymous callers with 401;
    /// - admin-only endpoint groups are verified to reject under-privileged personas with 403.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AuthorizationMatrixE2ETests : BaseE2ETest
    {
        protected override bool UseTestAuthentication => false;

        /// <summary>
        /// Endpoints that are deliberately reachable without authentication.
        /// Adding an anonymous endpoint requires consciously extending this list.
        /// </summary>
        private static readonly HashSet<string> KnownAnonymousEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            "POST api/auth/register",
            "GET api/auth/invite/{token}",
            "POST api/auth/login",
            "POST api/auth/forgot-password",
            "POST api/auth/reset-password",
            "POST api/auth/accept-invite",
            "GET api/HealthCheck"
        };

        [TestMethod]
        public void Anonymous_endpoints_must_match_the_explicit_allow_list()
        {
            var anonymous = DiscoverEndpoints()
                .Where(e => !e.RequiresAuthentication)
                .Select(e => e.Display)
                .ToList();

            anonymous.Should().BeEquivalentTo(
                KnownAnonymousEndpoints,
                "every anonymous endpoint must be a conscious decision recorded in the allow-list");
        }

        [TestMethod]
        public async Task Protected_endpoints_should_return_401Unauthorized_for_anonymous_callers()
        {
            UseAnonymous();
            var failures = new List<string>();

            foreach (var endpoint in DiscoverEndpoints().Where(e => e.RequiresAuthentication))
            {
                var response = await SendAsync(endpoint);
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    failures.Add($"{endpoint.Display} -> {(int)response.StatusCode}");
                }
            }

            failures.Should().BeEmpty("every protected endpoint must reject anonymous callers with 401");
        }

        [TestMethod]
        public async Task Global_admin_endpoints_should_return_403Forbidden_for_tenant_owners()
        {
            var owner = await RegisterTenantOwnerAsync();
            UseBearer(owner.AccessToken);

            var failures = new List<string>();
            foreach (var endpoint in DiscoverEndpoints().Where(e => e.Controller == "TenantsController"))
            {
                var response = await SendAsync(endpoint);
                if (response.StatusCode != HttpStatusCode.Forbidden)
                {
                    failures.Add($"{endpoint.Display} -> {(int)response.StatusCode}");
                }
            }

            failures.Should().BeEmpty("GlobalAdminOnly endpoints must reject tenant owners with 403");
        }

        [TestMethod]
        public async Task Tenant_admin_endpoints_should_return_403Forbidden_for_plain_members()
        {
            var owner = await RegisterTenantOwnerAsync();
            var member = await RegisterInvitedMemberAsync(owner.AccessToken);
            UseBearer(member.AccessToken);

            var failures = new List<string>();
            foreach (var endpoint in DiscoverEndpoints().Where(e => e.Controller == "TenantAdminController"))
            {
                var response = await SendAsync(endpoint);
                if (response.StatusCode != HttpStatusCode.Forbidden)
                {
                    failures.Add($"{endpoint.Display} -> {(int)response.StatusCode}");
                }
            }

            failures.Should().BeEmpty("TenantMemberManagement endpoints must reject plain members with 403");
        }

        [TestMethod]
        public async Task Permission_gated_mutations_should_return_403Forbidden_for_plain_members()
        {
            var owner = await RegisterTenantOwnerAsync();
            var member = await RegisterInvitedMemberAsync(owner.AccessToken);
            UseBearer(member.AccessToken);

            // Mutations guarded by [RequirePermissions] that the default member role lacks
            var endpoints = new (string Method, string Route)[]
            {
                ("POST", "api/Roles"),
                ("DELETE", "api/Roles/SomeRole"),
                ("PUT", "api/Roles/SomeRole/permissions"),
                ("PUT", "api/users/999999/roles"),
                ("POST", "api/Roles/reset-to-default"),
                ("PUT", "api/Configurations/Logging.Enabled")
            };

            var failures = new List<string>();
            foreach (var (method, route) in endpoints)
            {
                var response = await _sutClient.SendAsync(BuildRequest(method, route));
                if (response.StatusCode != HttpStatusCode.Forbidden)
                {
                    failures.Add($"{method} {route} -> {(int)response.StatusCode}");
                }
            }

            failures.Should().BeEmpty("permission-gated mutations must reject members without the permission with 403");
        }

        #region Endpoint discovery
        private sealed record DiscoveredEndpoint(string Controller, string Method, string Route, bool RequiresAuthentication)
        {
            public string Display => $"{Method} {Route}";
        }

        private static List<DiscoveredEndpoint> DiscoverEndpoints()
        {
            var endpoints = new List<DiscoveredEndpoint>();
            var controllers = typeof(MindedExample.Api.Startup).Assembly
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

            foreach (var controller in controllers)
            {
                var controllerRoutes = controller.GetCustomAttributes<RouteAttribute>(inherit: false)
                    .Select(r => r.Template)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
                if (controllerRoutes.Count == 0)
                {
                    controllerRoutes.Add(string.Empty);
                }

                var controllerName = controller.Name.Replace("Controller", string.Empty);
                bool controllerAuthorized = controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any();

                foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var httpAttribute = action.GetCustomAttributes().OfType<HttpMethodAttribute>().FirstOrDefault();
                    if (httpAttribute == null)
                    {
                        continue;
                    }

                    bool actionAllowsAnonymous = action.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any();
                    bool actionAuthorized = action.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any();
                    bool requiresAuth = !actionAllowsAnonymous && (controllerAuthorized || actionAuthorized);

                    var httpMethod = httpAttribute.HttpMethods.First();
                    var template = httpAttribute.Template;

                    if (!string.IsNullOrEmpty(template) && template.StartsWith('/'))
                    {
                        // Absolute template overrides the controller route
                        endpoints.Add(new DiscoveredEndpoint(
                            controller.Name, httpMethod, Normalize(template, controllerName), requiresAuth));
                        continue;
                    }

                    foreach (var controllerRoute in controllerRoutes)
                    {
                        var route = string.IsNullOrEmpty(template)
                            ? controllerRoute
                            : $"{controllerRoute}/{template}";
                        endpoints.Add(new DiscoveredEndpoint(
                            controller.Name, httpMethod, Normalize(route, controllerName), requiresAuth));
                    }
                }
            }

            return endpoints;
        }

        private static string Normalize(string template, string controllerName)
            => template.TrimStart('/').Replace("[controller]", controllerName);
        #endregion

        #region Request helpers
        private async Task<HttpResponseMessage> SendAsync(DiscoveredEndpoint endpoint)
        {
            // Substitute every route parameter with a syntactically valid value
            var path = Regex.Replace(endpoint.Route, @"\{[^}]+\}", "999999");
            return await _sutClient.SendAsync(BuildRequest(endpoint.Method, path));
        }

        private static HttpRequestMessage BuildRequest(string method, string path)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), "/" + path);
            if (method is "POST" or "PUT" or "PATCH" or "DELETE")
            {
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            }

            return request;
        }
        #endregion
    }
}
