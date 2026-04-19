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

namespace MindedExample.Application.Role.UnitTests
{
    [TestClass]
    public class AssignRolesToUserCommandValidatorTest
    {
        private AssignRolesToUserCommandValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new AssignRolesToUserCommandValidator();
        }

        [TestMethod]
        public async Task Validation_Succeeds_WhenUserIdAndRolesAreValid()
        {
            var command = new AssignRolesToUserCommand(1, new List<string> { "Admin" });

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_Fails_WhenUserIdIsZero()
        {
            var command = new AssignRolesToUserCommand(0, new List<string> { "Admin" });

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.UserId) &&
                e.Severity == Severity.Error)
                .Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_Fails_WhenRoleNamesIsNull()
        {
            var command = new AssignRolesToUserCommand(1, null);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.RoleNames) &&
                e.Severity == Severity.Error)
                .Should().BeTrue();
        }
    }
}
