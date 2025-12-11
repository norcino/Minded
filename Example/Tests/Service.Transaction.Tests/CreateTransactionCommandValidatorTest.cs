using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using Moq;
using Service.Transaction.Command;
using Service.Transaction.Validator;
using Minded.Framework.Mediator;
using FluentAssertions;

namespace Service.Transaction.Tests
{
    /// <summary>
    /// Unit tests for CreateTransactionCommandValidator.
    /// Tests validation rules for creating new transactions including null checks, ID validation, and category existence.
    /// </summary>
    [TestClass]
    public class CreateTransactionCommandValidatorTest
    {
        private CreateTransactionCommandValidator _sut;
        private Mock<IValidator<Data.Entity.Transaction>> _transactionValidatorMock;
        private Mock<IMediator> _mediatorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _transactionValidatorMock = new Mock<IValidator<Data.Entity.Transaction>>();
            _mediatorMock = new Mock<IMediator>();
            _sut = new CreateTransactionCommandValidator(_transactionValidatorMock.Object, _mediatorMock.Object);

            _transactionValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<Data.Entity.Transaction>()))
                .ReturnsAsync(new ValidationResult());
        }

        /// <summary>
        /// Verifies that validation succeeds when transaction is valid for creation.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenTransactionIsValidForCreation()
        {
            Data.Entity.Transaction transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 0;
                    t.CategoryId = 1;
                    t.Description = "Test transaction";
                    t.Credit = 100;
                });
            var command = new CreateTransactionCommand(transaction);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<Service.Category.Query.ExistsCategoryByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation fails when transaction in command is null.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenTransactionIsNull()
        {
            var command = new CreateTransactionCommand(null);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.Transaction) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when transaction has an ID (should be 0 for creation).
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenTransactionHasId()
        {
            Data.Entity.Transaction transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 5;
                    t.CategoryId = 1;
                    t.Description = "Test transaction";
                });
            var command = new CreateTransactionCommand(transaction);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.Transaction.Id) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} should not be specified on creation")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when CategoryId is zero.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenCategoryIdIsZero()
        {
            Data.Entity.Transaction transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 0;
                    t.CategoryId = 0;
                    t.Description = "Test transaction";
                });
            var command = new CreateTransactionCommand(transaction);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.Transaction.CategoryId) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when category does not exist.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenCategoryDoesNotExist()
        {
            Data.Entity.Transaction transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 0;
                    t.CategoryId = 999;
                    t.Description = "Test transaction";
                    t.Credit = 100;
                });
            var command = new CreateTransactionCommand(transaction);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<Service.Category.Query.ExistsCategoryByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.Transaction.CategoryId) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} references a non-existing category")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation merges results from transaction validator.
        /// </summary>
        [TestMethod]
        public async Task Validation_MergesResultsFromTransactionValidator()
        {
            Data.Entity.Transaction transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 0;
                    t.CategoryId = 1;
                    t.Description = "";
                });
            var command = new CreateTransactionCommand(transaction);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<Service.Category.Query.ExistsCategoryByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var transactionValidationResult = new ValidationResult();
            transactionValidationResult.OutcomeEntries.Add(
                new OutcomeEntry(nameof(transaction.Description), "{0} is mandatory"));
            _transactionValidatorMock.Setup(v => v.ValidateAsync(transaction))
                .ReturnsAsync(transactionValidationResult);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(transaction.Description) &&
                e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }
    }
}

