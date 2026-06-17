using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.User.Query;
using MindedExample.Application.User.Validator;

namespace MindedExample.Application.User.UnitTests
{
    [TestClass]
    public class GetInviteDetailsQueryValidatorTest
    {
        private GetInviteDetailsQueryValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new GetInviteDetailsQueryValidator();
        }

        [TestMethod]
        public async Task Validation_succeeds_when_token_or_code_is_provided()
        {
            var query = new GetInviteDetailsQuery("abc123");
            IValidationResult result = await _sut.ValidateAsync(query);

            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.OutcomeEntries.Any());
        }

        [TestMethod]
        public async Task Validation_fails_when_token_or_code_is_null()
        {
            var query = new GetInviteDetailsQuery(null);
            IValidationResult result = await _sut.ValidateAsync(query);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(query.TokenOrCode) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is mandatory"));
        }

        [TestMethod]
        public async Task Validation_fails_when_token_or_code_is_empty_string()
        {
            var query = new GetInviteDetailsQuery("   ");
            IValidationResult result = await _sut.ValidateAsync(query);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e => e.PropertyName == nameof(query.TokenOrCode) && e.Severity == Severity.Error));
        }
    }
}
