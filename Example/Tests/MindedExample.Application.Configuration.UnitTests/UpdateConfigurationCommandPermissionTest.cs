using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Authorization.Attributes;
using MindedExample.Application.Configuration.Command;
using MindedExample.Domain;

namespace MindedExample.Application.Configuration.UnitTests
{
    [TestClass]
    public class UpdateConfigurationCommandPermissionTest
    {
        [TestMethod]
        public void UpdateConfigurationCommand_RequiresCanUpdateConfigurationPermission()
        {
            var attr = typeof(UpdateConfigurationCommand)
                .GetCustomAttribute<RequirePermissionsAttribute>();

            attr.Should().NotBeNull();
            attr.Permissions.Should().Contain(Permissions.CanUpdateConfiguration);
        }
    }
}
