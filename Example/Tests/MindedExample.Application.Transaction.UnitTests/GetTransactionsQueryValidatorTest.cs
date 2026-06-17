using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using MindedExample.Application.Transaction.Query;
using MindedExample.Application.Transaction.Validator;

namespace MindedExample.Application.Transaction.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="GetTransactionsQueryValidator"/>.
    /// The validator performs no structural validation of Top — values exceeding the
    /// maximum page size (100) are silently capped by the handler before ApplyQueryTo is called.
    /// </summary>
    [TestClass]
    public class GetTransactionsQueryValidatorTest
    {
        private GetTransactionsQueryValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new GetTransactionsQueryValidator();
        }

        [TestMethod]
        public async Task Validation_Succeeds_WhenTopIsNull()
        {
            var query = new GetTransactionsQuery { Top = null };

            IValidationResult result = await _sut.ValidateAsync(query);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Validation_Succeeds_WhenTopIsWithinLimit()
        {
            var query = new GetTransactionsQuery { Top = 50 };

            IValidationResult result = await _sut.ValidateAsync(query);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Validation_Succeeds_WhenTopIsExactlyAtMaximum()
        {
            var query = new GetTransactionsQuery { Top = 100 };

            IValidationResult result = await _sut.ValidateAsync(query);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Values above the maximum are NOT rejected by the validator — capping happens in the handler.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenTopExceedsMaximum()
        {
            var query = new GetTransactionsQuery { Top = 101 };

            IValidationResult result = await _sut.ValidateAsync(query);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Validation_Succeeds_WhenTopIsZero()
        {
            var query = new GetTransactionsQuery { Top = 0 };

            IValidationResult result = await _sut.ValidateAsync(query);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }
    }
}
