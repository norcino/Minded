using System;
using System.Linq;
using System.Threading.Tasks;
using AnonymousData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Service.Category.Query;
using Service.Category.Validator;
using FluentAssertions;

namespace Service.Category.Tests
{
    /// <summary>
    /// Unit tests for GetCategoriesQueryValidator.
    /// Tests validation rules for GetCategoriesQuery including Top parameter limits.
    /// </summary>
    [TestClass]
    public class GetCategoriesQueryValidatorTest
    {
        private GetCategoriesQueryValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new GetCategoriesQueryValidator();
        }

        /// <summary>
        /// Verifies that validation succeeds when Top is null.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenTopIsNull()
        {
            var query = new GetCategoriesQuery
            {
                Top = null
            };

            var result = await _sut.ValidateAsync(query);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation succeeds when Top is within allowed limit.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenTopIsWithinLimit()
        {
            var query = new GetCategoriesQuery
            {
                Top = 50
            };

            var result = await _sut.ValidateAsync(query);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation succeeds when Top is exactly at the maximum limit.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenTopIsExactlyAtMaximum()
        {
            var query = new GetCategoriesQuery
            {
                Top = 100
            };

            var result = await _sut.ValidateAsync(query);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation fails when Top exceeds maximum allowed value.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenTopExceedsMaximum()
        {
            var query = new GetCategoriesQuery
            {
                Top = 101
            };

            var result = await _sut.ValidateAsync(query);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(query.Top) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is above teh maximum allowed 100")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when Top is significantly above maximum.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenTopIsSignificantlyAboveMaximum()
        {
            var query = new GetCategoriesQuery
            {
                Top = 500
            };

            var result = await _sut.ValidateAsync(query);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(query.Top) &&
                e.Severity == Severity.Error)
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation succeeds when Top is zero.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenTopIsZero()
        {
            var query = new GetCategoriesQuery
            {
                Top = 0
            };

            var result = await _sut.ValidateAsync(query);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }
    }
}

