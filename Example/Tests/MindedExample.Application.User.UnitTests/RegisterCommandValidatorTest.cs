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
    public class RegisterCommandValidatorTest
    {
        private RegisterCommandValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new RegisterCommandValidator();
        }

        [TestMethod]
        public async Task Validation_succeeds_when_all_required_fields_are_provided_for_create_tenant()
        {
            var command = new RegisterCommand("John", "Doe", "john@example.com", "Secret1!");
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.OutcomeEntries.Any());
        }

        [TestMethod]
        public async Task Validation_fails_when_name_is_missing()
        {
            var command = new RegisterCommand(null, "Doe", "john@example.com", "Secret1!");
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e => e.PropertyName == nameof(command.Name) && e.Severity == Severity.Error));
        }

        [TestMethod]
        public async Task Validation_fails_when_email_is_missing()
        {
            var command = new RegisterCommand("John", "Doe", null, "Secret1!");
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e => e.PropertyName == nameof(command.Email) && e.Severity == Severity.Error));
        }

        [TestMethod]
        public async Task Validation_fails_when_password_is_missing()
        {
            var command = new RegisterCommand("John", "Doe", "john@example.com", null);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e => e.PropertyName == nameof(command.Password) && e.Severity == Severity.Error));
        }

        [TestMethod]
        public async Task Validation_fails_when_mode_is_join_tenant_and_tenant_name_is_missing()
        {
            var command = new RegisterCommand("John", "Doe", "john@example.com", "Secret1!", tenantName: null, mode: "join-tenant");
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e => e.PropertyName == nameof(command.TenantName) && e.Severity == Severity.Error));
        }

        [TestMethod]
        public async Task Validation_succeeds_when_mode_is_join_tenant_and_tenant_name_is_provided()
        {
            var command = new RegisterCommand("John", "Doe", "john@example.com", "Secret1!", tenantName: "Acme", mode: "join-tenant");
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsTrue(result.IsValid);
        }
    }
}
