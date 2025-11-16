using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Service.Transaction.Validator;
using Builder;
using FluentAssertions;
using Minded.Framework.CQRS.Abstractions;

namespace Service.Transaction.Tests
{
    /// <summary>
    /// Unit tests for TransactionValidator.
    /// Tests validation rules for Transaction entity including Description, Credit/Debit, and CategoryId.
    /// </summary>
    [TestClass]
    public class TransactionValidatorTest
    {
        private TransactionValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new TransactionValidator();
        }

        /// <summary>
        /// Verifies that validation succeeds when all transaction properties are valid.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenTransactionIsValid()
        {
            var transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.Description = "Valid description";
                    t.Credit = 100;
                    t.Debit = 0;
                    t.CategoryId = 1;
                });

            var result = await _sut.ValidateAsync(transaction);

            result.OutcomeEntries.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when Description is null.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenDescriptionIsNull()
        {
            var transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t => t.Description = null);

            var result = await _sut.ValidateAsync(transaction);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                    e.PropertyName == nameof(transaction.Description) &&
                    e.Severity == Severity.Error &&
                    e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when Description is empty string.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenDescriptionIsEmpty()
        {
            var transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t => t.Description = "");

            var result = await _sut.ValidateAsync(transaction);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                    e.PropertyName == nameof(transaction.Description) &&
                    e.Severity == Severity.Error &&
                    e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when Description is only whitespace.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenDescriptionIsWhitespace()
        {
            var transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t => t.Description = "    ");

            var result = await _sut.ValidateAsync(transaction);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                    e.PropertyName == nameof(transaction.Description) &&
                    e.Severity == Severity.Error &&
                    e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when both Credit and Debit are zero.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenBothCreditAndDebitAreZero()
        {
            var transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.Credit = 0;
                    t.Debit = 0;
                    t.CategoryId = 1;
                });

            var result = await _sut.ValidateAsync(transaction);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                    e.PropertyName == nameof(Data.Entity.Transaction) &&
                    e.Severity == Severity.Error &&
                    e.Message == "{0} must have either a Debit or Credit value")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation succeeds when Credit has value and Debit is zero.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenCreditHasValueAndDebitIsZero()
        {
            var transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.Credit = 100;
                    t.Debit = 0;
                    t.CategoryId = 1;
                });

            var result = await _sut.ValidateAsync(transaction);

            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation succeeds when Debit has value and Credit is zero.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenDebitHasValueAndCreditIsZero()
        {
            var transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.Credit = 0;
                    t.Debit = 50;
                    t.CategoryId = 1;
                });

            var result = await _sut.ValidateAsync(transaction);

            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when CategoryId is zero.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenCategoryIdIsZero()
        {
            var transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.CategoryId = 0;
                    t.Credit = 100;
                });

            var result = await _sut.ValidateAsync(transaction);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                    e.PropertyName == nameof(transaction.CategoryId) &&
                    e.Severity == Severity.Error &&
                    e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation succeeds when CategoryId has a valid value.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenCategoryIdIsValid()
        {
            var transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.CategoryId = 5;
                    t.Credit = 100;
                });

            var result = await _sut.ValidateAsync(transaction);

            result.IsValid.Should().BeTrue();
        }
    }
}

