using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Command;
using MindedExample.Application.User.Validator;

namespace MindedExample.Application.User.UnitTests
{
    [TestClass]
    public class LoginCommandValidatorTest
    {
        private LoginCommandValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new LoginCommandValidator();
        }

        [TestMethod]
        public async Task Validation_succeeds_when_email_and_password_are_provided()
        {
            var command = new LoginCommand("john@example.com", "Secret1!");
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.OutcomeEntries.Any());
        }

        [TestMethod]
        public async Task Validation_fails_when_email_is_missing()
        {
            var command = new LoginCommand(null, "Secret1!");
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e => e.PropertyName == nameof(command.Email) && e.Severity == Severity.Error));
        }

        [TestMethod]
        public async Task Validation_fails_when_password_is_missing()
        {
            var command = new LoginCommand("john@example.com", null);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e => e.PropertyName == nameof(command.Password) && e.Severity == Severity.Error));
        }

        [TestMethod]
        public async Task Validation_fails_when_both_fields_are_missing()
        {
            var command = new LoginCommand(null, null);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(2, result.OutcomeEntries.Count);
        }
    }
}
