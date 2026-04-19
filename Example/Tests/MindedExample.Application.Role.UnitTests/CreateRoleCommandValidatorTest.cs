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
    public class CreateRoleCommandValidatorTest
    {
        private CreateRoleCommandValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new CreateRoleCommandValidator();
        }

        [TestMethod]
        public async Task Validation_Succeeds_WhenRoleNameIsValid()
        {
            var command = new CreateRoleCommand("NewRole");

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Validation_Fails_WhenRoleNameIsEmpty()
        {
            var command = new CreateRoleCommand("");

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
            var command = new CreateRoleCommand(null);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
        }
    }
}
