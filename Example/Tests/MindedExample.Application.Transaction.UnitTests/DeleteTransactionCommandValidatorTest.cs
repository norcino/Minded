using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.Mediator;
using Moq;
using MindedExample.Application.Transaction.Command;
using MindedExample.Application.Transaction.Query;
using MindedExample.Application.Transaction.Validator;

namespace MindedExample.Application.Transaction.UnitTests
{
    /// <summary>
    /// Unit tests for DeleteTransactionCommandValidator.
    /// Tests validation rules for deleting transactions including existence checks.
    /// The validator delegates the existence check to <see cref="ExistsTransactionByIdQuery"/> via IMediator,
    /// so these tests mock the mediator rather than the DbContext.
    /// </summary>
    [TestClass]
    public class DeleteTransactionCommandValidatorTest
    {
        private DeleteTransactionCommandValidator _sut;
        private Mock<IMediator> _mediatorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _mediatorMock = new Mock<IMediator>();
            _sut = new DeleteTransactionCommandValidator(_mediatorMock.Object);
        }

        /// <summary>
        /// Verifies that validation succeeds when the transaction exists.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenTransactionExists()
        {
            var transactionId = Any.Int();
            var command = new DeleteTransactionCommand(transactionId);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation fails when the transaction does not exist.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenTransactionDoesNotExist()
        {
            var transactionId = Any.Int();
            var command = new DeleteTransactionCommand(transactionId);

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
        /// Verifies that the validator dispatches ExistsTransactionByIdQuery with the correct transaction ID.
        /// </summary>
        [TestMethod]
        public async Task Validation_Dispatches_ExistsTransactionByIdQuery_WithCorrectId()
        {
            var transactionId = 42;
            var command = new DeleteTransactionCommand(transactionId);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await _sut.ValidateAsync(command);

            _mediatorMock.Verify(m => m.ProcessQueryAsync(
                It.Is<ExistsTransactionByIdQuery>(q => q.TransactionId == transactionId),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

