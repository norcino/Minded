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
    public class AcceptInviteCommandValidatorTest
    {
        private AcceptInviteCommandValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new AcceptInviteCommandValidator();
        }

        [TestMethod]
        public async Task Validation_succeeds_when_all_required_fields_are_provided()
        {
            var command = new AcceptInviteCommand("abc123", "john@example.com", "John", "Doe", "Secret1!");
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.OutcomeEntries.Any());
        }

        [TestMethod]
        public async Task Validation_fails_when_code_or_token_is_missing()
        {
            var command = new AcceptInviteCommand(null, "john@example.com", "John", "Doe", "Secret1!");
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e => e.PropertyName == nameof(command.CodeOrToken) && e.Severity == Severity.Error));
        }

        [TestMethod]
        public async Task Validation_fails_when_email_is_missing()
        {
            var command = new AcceptInviteCommand("abc123", null, "John", "Doe", "Secret1!");
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e => e.PropertyName == nameof(command.Email) && e.Severity == Severity.Error));
        }

        [TestMethod]
        public async Task Validation_fails_when_password_is_missing()
        {
            var command = new AcceptInviteCommand("abc123", "john@example.com", "John", "Doe", null);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e => e.PropertyName == nameof(command.Password) && e.Severity == Severity.Error));
        }

        [TestMethod]
        public async Task Validation_fails_when_all_required_fields_are_missing()
        {
            var command = new AcceptInviteCommand(null, null, null, null, null);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(3, result.OutcomeEntries.Count);
        }
    }
}
