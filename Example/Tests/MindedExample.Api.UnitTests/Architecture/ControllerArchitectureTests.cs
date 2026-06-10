using System;
using System.Collections.Generic;
using System.Linq;
using MindedExample.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MindedExample.Api.UnitTests.Architecture
{
    [TestClass]
    public class ControllerArchitectureTests
    {
        // Temporary allowlist for legacy controllers that are pending CQRS/controller refactors.
        // Keep this list small and remove entries as refactors are completed.
        private static readonly HashSet<string> LegacyControllerAllowlist = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(AuthController),
            nameof(TenantAdminController),
            nameof(UsersController)
        };

        [TestMethod]
        public void Controllers_outside_legacy_allowlist_should_not_depend_on_IMindedExampleContext()
        {
            var controllerTypes = typeof(TenantsController).Assembly
                .GetTypes()
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    typeof(ControllerBase).IsAssignableFrom(t));

            var violatingControllers = controllerTypes
                .Where(t => !LegacyControllerAllowlist.Contains(t.Name))
                .Where(t => t.GetConstructors()
                    .SelectMany(c => c.GetParameters())
                    .Any(p => p.ParameterType.Name == "IMindedExampleContext"))
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToList();

            Assert.AreEqual(
                0,
                violatingControllers.Count,
                "These controllers still depend on IMindedExampleContext: " + string.Join(", ", violatingControllers));
        }

        [TestMethod]
        public void TenantsController_should_be_thin_and_depend_on_RestMediator_only()
        {
            var constructorParameters = typeof(TenantsController)
                .GetConstructors()
                .Single()
                .GetParameters()
                .Select(p => p.ParameterType.Name)
                .ToList();

            CollectionAssert.Contains(constructorParameters, "IRestMediator");
            CollectionAssert.DoesNotContain(constructorParameters, "IMindedExampleContext");
            CollectionAssert.DoesNotContain(constructorParameters, "IMediator");
        }
    }
}
