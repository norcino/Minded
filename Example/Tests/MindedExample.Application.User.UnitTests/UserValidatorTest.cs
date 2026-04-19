using System.Linq;
using System.Threading.Tasks;
using Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Validator;

namespace MindedExample.Application.User.UnitTests
{
    [TestClass]
    public class UserValidatorTest
    {
        private UserValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new UserValidator();
        }

        [TestMethod]
        public async Task Validation_succeed_when_user_is_valid()
        {
            MindedExample.Domain.User user = Builder<MindedExample.Domain.User>.New()
                .Build(e => { 
                    e.Name = "John"; 
                    e.Surname = "Doe"; 
                    e.Email = "john.doe@example.com"; 
                });
            IValidationResult result = await _sut.ValidateAsync(user);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.OutcomeEntries.Any());
        }

        [TestMethod]
        public async Task Validation_fails_when_name_is_null()
        {
            MindedExample.Domain.User user = Builder<MindedExample.Domain.User>.New()
                .Build(e => { 
                    e.Name = null; 
                    e.Surname = "Doe"; 
                    e.Email = "john.doe@example.com"; 
                });
            IValidationResult result = await _sut.ValidateAsync(user);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(user.Name) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is mandatory"));
        }

        [TestMethod]
        public async Task Validation_fails_when_surname_is_null()
        {
            MindedExample.Domain.User user = Builder<MindedExample.Domain.User>.New()
                .Build(e => { 
                    e.Name = "John"; 
                    e.Surname = null; 
                    e.Email = "john.doe@example.com"; 
                });
            IValidationResult result = await _sut.ValidateAsync(user);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(user.Surname) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is mandatory"));
        }

        [TestMethod]
        public async Task Validation_fails_when_email_is_null()
        {
            MindedExample.Domain.User user = Builder<MindedExample.Domain.User>.New()
                .Build(e => { 
                    e.Name = "John"; 
                    e.Surname = "Doe"; 
                    e.Email = null; 
                });
            IValidationResult result = await _sut.ValidateAsync(user);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(user.Email) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is mandatory"));
        }

        [TestMethod]
        public async Task Validation_fails_when_email_is_invalid()
        {
            MindedExample.Domain.User user = Builder<MindedExample.Domain.User>.New()
                .Build(e => { 
                    e.Name = "John"; 
                    e.Surname = "Doe"; 
                    e.Email = "invalid-email"; 
                });
            IValidationResult result = await _sut.ValidateAsync(user);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(user.Email) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is not a valid email address"));
        }
    }
}

