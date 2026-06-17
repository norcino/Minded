using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.Role.Command;
using MindedExample.Application.Role.Validator;
using MindedExample.Domain;

namespace MindedExample.Application.Role.UnitTests
{
    [TestClass]
    public class UpdateRolePermissionsCommandValidatorTest
    {
        private UpdateRolePermissionsCommandValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new UpdateRolePermissionsCommandValidator();
        }

        [TestMethod]
        public async Task Validation_Succeeds_WhenRoleNameAndPermissionsAreValid()
        {
            var command = new UpdateRolePermissionsCommand("CustomRole", new List<string> { Permissions.CanCreateCategory });

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_Fails_WhenRoleNameIsEmpty()
        {
            var command = new UpdateRolePermissionsCommand("", new List<string> { Permissions.CanCreateCategory });

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.RoleName) &&
                e.Severity == Severity.Error)
                .Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_Fails_WhenPermissionNamesIsNull()
        {
            var command = new UpdateRolePermissionsCommand("CustomRole", null);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.PermissionNames) &&
                e.Severity == Severity.Error)
                .Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_Succeeds_WhenAdminRoleKeepsAllProtectedPermissions()
        {
            var allPermissions = new List<string>(Permissions.ProtectedAdminPermissions)
            {
                Permissions.CanCreateCategory,
                Permissions.CanDeleteCategory
            };
            var command = new UpdateRolePermissionsCommand(Roles.Admin, allPermissions);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_Fails_WhenAdminRoleIsMissingProtectedPermission()
        {
            // Include all protected permissions except CanManageRoles
            var permissions = Permissions.ProtectedAdminPermissions
                .Where(p => p != Permissions.CanManageRoles)
                .ToList();
            var command = new UpdateRolePermissionsCommand(Roles.Admin, permissions);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.PermissionNames) &&
                e.Message.Contains(Permissions.CanManageRoles) &&
                e.Severity == Severity.Error)
                .Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_Fails_WhenAdminRoleIsMissingMultipleProtectedPermissions()
        {
            var command = new UpdateRolePermissionsCommand(Roles.Admin, new List<string> { Permissions.CanCreateCategory });

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            var entry = result.OutcomeEntries.First(e => e.PropertyName == nameof(command.PermissionNames));
            foreach (var protectedPerm in Permissions.ProtectedAdminPermissions)
            {
                entry.Message.Should().Contain(protectedPerm);
            }
        }

        [TestMethod]
        public async Task Validation_Succeeds_WhenNonAdminRoleHasNoProtectedPermissions()
        {
            var command = new UpdateRolePermissionsCommand("CustomRole", new List<string> { Permissions.CanCreateCategory });

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_Succeeds_WhenNonAdminRoleHasEmptyPermissions()
        {
            var command = new UpdateRolePermissionsCommand("CustomRole", new List<string>());

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }
    }
}
