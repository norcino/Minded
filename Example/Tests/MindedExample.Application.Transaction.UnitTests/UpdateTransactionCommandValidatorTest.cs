using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Builder;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.Mediator;
using Moq;
using MindedExample.Application.Category.Query;
using MindedExample.Application.Transaction.Command;
using MindedExample.Application.Transaction.Query;
using MindedExample.Application.Transaction.Validator;
using MindedExample.Application.User.Query;

namespace MindedExample.Application.Transaction.UnitTests
{
    /// <summary>
    /// Unit tests for UpdateTransactionCommandValidator.
    /// Tests validation rules for updating transactions including existence checks and ID matching.
    /// </summary>
    [TestClass]
    public class UpdateTransactionCommandValidatorTest
    {
        private UpdateTransactionCommandValidator _sut;
        private Mock<IValidator<MindedExample.Domain.Transaction>> _transactionValidatorMock;
        private Mock<IMediator> _mediatorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _transactionValidatorMock = new Mock<IValidator<MindedExample.Domain.Transaction>>();
            _mediatorMock = new Mock<IMediator>();
            _sut = new UpdateTransactionCommandValidator(_transactionValidatorMock.Object, _mediatorMock.Object);

            _transactionValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<MindedExample.Domain.Transaction>()))
                .ReturnsAsync(new ValidationResult());
        }

        /// <summary>
        /// Verifies that validation succeeds when transaction exists and is valid for update.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenTransactionExistsAndIsValid()
        {
            MindedExample.Domain.Transaction transaction = Builder<MindedExample.Domain.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 5;
                    t.CategoryId = 1;
                    t.UserId = 1;
                    t.Description = "Updated transaction";
                    t.Credit = 200;
                });
            var command = new UpdateTransactionCommand(5, transaction);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsCategoryInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsUserInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation fails when the category does not exist.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenCategoryDoesNotExist()
        {
            MindedExample.Domain.Transaction transaction = Builder<MindedExample.Domain.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 5;
                    t.CategoryId = 999;
                    t.UserId = 1;
                    t.Description = "Updated transaction";
                    t.Credit = 200;
                });
            var command = new UpdateTransactionCommand(5, transaction);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsCategoryInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsUserInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.Transaction.CategoryId) &&
                e.Severity == Severity.Error &&
                e.Message == "Category with ID {0} does not exist")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when the user does not exist in the current tenant.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenUserDoesNotExistInCurrentTenant()
        {
            MindedExample.Domain.Transaction transaction = Builder<MindedExample.Domain.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 5;
                    t.CategoryId = 1;
                    t.UserId = 999;
                    t.Description = "Updated transaction";
                    t.Credit = 200;
                });
            var command = new UpdateTransactionCommand(5, transaction);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsCategoryInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsUserInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.Transaction.UserId) &&
                e.Severity == Severity.Error &&
                e.Message == "User with ID {0} does not exist in the current tenant")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when transaction in command is null.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenTransactionIsNull()
        {
            var command = new UpdateTransactionCommand(5, null);

            IValidationResult result = await _sut.ValidateAsync(command);

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
            MindedExample.Domain.Transaction transaction = Builder<MindedExample.Domain.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 10;
                    t.CategoryId = 1;
                    t.Description = "Test transaction";
                });
            var command = new UpdateTransactionCommand(5, transaction);

            IValidationResult result = await _sut.ValidateAsync(command);

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
            MindedExample.Domain.Transaction transaction = Builder<MindedExample.Domain.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 999;
                    t.CategoryId = 1;
                    t.Description = "Test transaction";
                    t.Credit = 100;
                });
            var command = new UpdateTransactionCommand(999, transaction);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            IValidationResult result = await _sut.ValidateAsync(command);

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
            MindedExample.Domain.Transaction transaction = Builder<MindedExample.Domain.Transaction>.New()
                .Build(t =>
                {
                    t.Id = 5;
                    t.CategoryId = 1;
                    t.Description = "";
                });
            var command = new UpdateTransactionCommand(5, transaction);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsCategoryInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsUserInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
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

