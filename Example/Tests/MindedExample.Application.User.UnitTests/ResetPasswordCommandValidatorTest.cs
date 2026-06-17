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
    public class ResetPasswordCommandValidatorTest
    {
        private ResetPasswordCommandValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new ResetPasswordCommandValidator();
        }

        [TestMethod]
        public async Task Validation_succeeds_when_token_and_new_password_are_provided()
        {
            var command = new ResetPasswordCommand("abc123", "NewSecret1!");
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.OutcomeEntries.Any());
        }

        [TestMethod]
        public async Task Validation_fails_when_token_is_missing()
        {
            var command = new ResetPasswordCommand(null, "NewSecret1!");
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e => e.PropertyName == nameof(command.Token) && e.Severity == Severity.Error));
        }

        [TestMethod]
        public async Task Validation_fails_when_new_password_is_missing()
        {
            var command = new ResetPasswordCommand("abc123", null);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e => e.PropertyName == nameof(command.NewPassword) && e.Severity == Severity.Error));
        }

        [TestMethod]
        public async Task Validation_fails_when_both_fields_are_missing()
        {
            var command = new ResetPasswordCommand(null, null);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(2, result.OutcomeEntries.Count);
        }
    }
}
