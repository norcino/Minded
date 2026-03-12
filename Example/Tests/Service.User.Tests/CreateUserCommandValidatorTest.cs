using System.Linq;
using System.Threading.Tasks;
using Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using Service.User.Command;
using Service.User.Validator;

namespace Service.User.Tests
{
    [TestClass]
    public class CreateUserCommandValidatorTest
    {
        private CreateUserCommandValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new CreateUserCommandValidator(new UserValidator());
        }

        [TestMethod]
        public async Task Validation_succeed_when_user_is_valid_for_creation()
        {
            Data.Entity.User user = Builder<Data.Entity.User>.New()
                .Build(e => { 
                    e.Name = "John"; 
                    e.Surname = "Doe"; 
                    e.Email = "john.doe@example.com"; 
                    e.Id = 0; 
                });
            var command = new CreateUserCommand(user);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.OutcomeEntries.Any());
        }

        [TestMethod]
        public async Task Validation_fails_when_user_in_command_is_null()
        {
            var command = new CreateUserCommand(null);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.User) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is mandatory"));
        }

        [TestMethod]
        public async Task Validation_fails_when_user_in_command_has_id()
        {
            Data.Entity.User user = Builder<Data.Entity.User>.New()
                .Build(e => {
                    e.Name = "John";
                    e.Surname = "Doe";
                    e.Email = "john.doe@example.com";
                    e.Id = 1;
                });
            
            var command = new CreateUserCommand(user);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.User.Id) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} should not be specified on creation"));
        }
    }
}

