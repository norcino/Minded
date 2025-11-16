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
    /// Unit tests for UpdateTransactionCommandValidator.
    /// Tests validation rules for updating transactions including existence checks and ID matching.
    /// </summary>
    [TestClass]
    public class UpdateTransactionCommandValidatorTest
    {
        private UpdateTransactionCommandValidator _sut;
        private Mock<IValidator<Data.Entity.Transaction>> _transactionValidatorMock;
        private Mock<IMediator> _mediatorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _transactionValidatorMock = new Mock<IValidator<Data.Entity.Transaction>>();
            _mediatorMock = new Mock<IMediator>();
            _sut = new UpdateTransactionCommandValidator(_transactionValidatorMock.Object, _mediatorMock.Object);

            _transactionValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<Data.Entity.Transaction>()))
                .ReturnsAsync(new ValidationResult());
        }

        /// <summary>
        /// Verifies that validation succeeds when transaction exists and is valid for update.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenTransactionExistsAndIsValid()
        {
            var transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 5;
                    t.CategoryId = 1;
                    t.Description = "Updated transaction";
                    t.Credit = 200;
                });
            var command = new UpdateTransactionCommand(5, transaction);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<Service.Transaction.Query.ExistsTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation fails when transaction in command is null.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenTransactionIsNull()
        {
            var command = new UpdateTransactionCommand(5, null);

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.Transaction) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when command ID does not match transaction entity ID.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenCommandIdDoesNotMatchTransactionId()
        {
            var transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 10;
                    t.CategoryId = 1;
                    t.Description = "Test transaction";
                });
            var command = new UpdateTransactionCommand(5, transaction);

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.TransactionId) &&
                e.Severity == Severity.Error &&
                e.Message == "Transaction ID in command does not match Transaction entity ID")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when transaction does not exist.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenTransactionDoesNotExist()
        {
            var transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 999;
                    t.CategoryId = 1;
                    t.Description = "Test transaction";
                    t.Credit = 100;
                });
            var command = new UpdateTransactionCommand(999, transaction);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<Service.Transaction.Query.ExistsTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.TransactionId) &&
                e.Severity == Severity.Error &&
                e.Message == "Transaction with ID {0} not found")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation merges results from transaction validator.
        /// </summary>
        [TestMethod]
        public async Task Validation_MergesResultsFromTransactionValidator()
        {
            var transaction = Builder<Data.Entity.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 5;
                    t.CategoryId = 1;
                    t.Description = "";
                });
            var command = new UpdateTransactionCommand(5, transaction);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<Service.Transaction.Query.ExistsTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var transactionValidationResult = new ValidationResult();
            transactionValidationResult.OutcomeEntries.Add(
                new OutcomeEntry(nameof(transaction.Description), "{0} is mandatory"));
            _transactionValidatorMock.Setup(v => v.ValidateAsync(transaction))
                .ReturnsAsync(transactionValidationResult);

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(transaction.Description) &&
                e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }
    }
}

