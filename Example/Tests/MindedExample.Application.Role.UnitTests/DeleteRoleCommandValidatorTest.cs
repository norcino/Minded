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
    public class DeleteRoleCommandValidatorTest
    {
        private DeleteRoleCommandValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new DeleteRoleCommandValidator();
        }

        [TestMethod]
        public async Task Validation_Succeeds_WhenRoleNameIsValid()
        {
            var command = new DeleteRoleCommand("CustomRole");

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Validation_Fails_WhenRoleNameIsEmpty()
        {
            var command = new DeleteRoleCommand("");

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.RoleName) &&
                e.Severity == Severity.Error)
                .Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_Fails_WhenRoleNameIsNull()
        {
            var command = new DeleteRoleCommand(null);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
        }

        [TestMethod]
        public async Task Validation_Fails_WhenDeletingAdminRole()
        {
            var command = new DeleteRoleCommand(Roles.Admin);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.RoleName) &&
                e.Message == "The Admin role cannot be deleted" &&
                e.Severity == Severity.Error)
                .Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_Succeeds_WhenDeletingUserRole()
        {
            var command = new DeleteRoleCommand(Roles.User);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }
    }
}
