using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using Service.User.Query;
using Service.User.Validator;

namespace Service.User.Tests
{
    [TestClass]
    public class GetUsersQueryValidatorTest
    {
        private GetUsersQueryValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new GetUsersQueryValidator();
        }

        [TestMethod]
        public async Task Validation_succeed_when_top_is_within_limit()
        {
            var query = new GetUsersQuery { Top = 50 };
            IValidationResult result = await _sut.ValidateAsync(query);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.OutcomeEntries.Any());
        }

        [TestMethod]
        public async Task Validation_fails_when_top_exceeds_limit()
        {
            var query = new GetUsersQuery { Top = 150 };
            IValidationResult result = await _sut.ValidateAsync(query);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(query.Top) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is above the maximum allowed 100"));
        }
    }
}

