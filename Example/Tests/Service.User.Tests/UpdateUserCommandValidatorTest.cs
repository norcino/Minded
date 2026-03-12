using System.Linq;
using System.Threading.Tasks;
using Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using Moq;
using Service.User.Command;
using Service.User.Validator;
using Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Service.User.Tests
{
    [TestClass]
    public class UpdateUserCommandValidatorTest
    {
        private UpdateUserCommandValidator _sut;
        private Mock<IMindedExampleContext> _contextMock;
        private Mock<DbSet<Data.Entity.User>> _userDbSetMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _contextMock = new Mock<IMindedExampleContext>();
            _userDbSetMock = new Mock<DbSet<Data.Entity.User>>();
            _contextMock.Setup(c => c.Users).Returns(_userDbSetMock.Object);
            _sut = new UpdateUserCommandValidator(new UserValidator(), _contextMock.Object);
        }

        [TestMethod]
        public async Task Validation_fails_when_user_in_command_is_null()
        {
            var command = new UpdateUserCommand(1, null);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.User) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is mandatory"));
        }

        [TestMethod]
        public async Task Validation_fails_when_user_id_does_not_match()
        {
            Data.Entity.User user = Builder<Data.Entity.User>.New()
                .Build(e => {
                    e.Name = "John";
                    e.Surname = "Doe";
                    e.Email = "john.doe@example.com";
                    e.Id = 2;
                });
            
            var command = new UpdateUserCommand(1, user);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.UserId) &&
                e.Severity == Severity.Error &&
                e.Message == "User ID in command does not match User entity ID"));
        }
    }
}

