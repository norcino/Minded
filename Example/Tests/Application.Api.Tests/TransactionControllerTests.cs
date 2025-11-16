using System.Threading;
using System.Threading.Tasks;
using Application.Api.Controllers;
using Data.Entity;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Service.Transaction.Command;
using Service.Transaction.Query;
using AnonymousData;
using Minded.Extensions.WebApi;
using Minded.Framework.CQRS.Command;

namespace Application.Api.Tests
{
    /// <summary>
    /// Unit tests for TransactionController.
    /// Tests verify that controller methods correctly construct commands/queries and pass them to RestMediator.
    /// </summary>
    [TestClass]
    public class TransactionControllerTests
    {
        private TransactionController _controller;
        private Mock<IRestMediator> _mediatorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _mediatorMock = new Mock<IRestMediator>();
            _controller = new TransactionController(_mediatorMock.Object);
        }

        [TestMethod]
        public async Task Get_with_ODataQueryOptions_invokes_ProcessRestQueryAsync_with_GetMany_operation_and_GetTransactionsQuery()
        {
            // Arrange
            // Note: ODataQueryOptions cannot be easily mocked, but the controller doesn't use it directly.
            // It's passed to GetTransactionsQuery constructor.
            // For this unit test, we verify the controller calls the mediator with the correct operation.
            var queryOptions = null as ODataQueryOptions<Transaction>;
            var cancellationToken = new CancellationToken();

            _mediatorMock
                .Setup(m => m.ProcessRestQueryAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<GetTransactionsQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            // Act
            await _controller.Get(queryOptions, cancellationToken);

            // Assert
            _mediatorMock.Verify(m => m.ProcessRestQueryAsync(
                RestOperation.GetMany,
                It.Is<GetTransactionsQuery>(q => q.Options == queryOptions),
                cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task Get_by_id_invokes_ProcessRestQueryAsync_with_GetSingle_operation_and_GetTransactionByIdQuery_with_correct_id()
        {
            // Arrange
            var transactionId = Any.Int();
            var cancellationToken = new CancellationToken();
            
            _mediatorMock
                .Setup(m => m.ProcessRestQueryAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<GetTransactionByIdQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            // Act
            await _controller.Get(transactionId, cancellationToken);

            // Assert
            _mediatorMock.Verify(m => m.ProcessRestQueryAsync(
                RestOperation.GetSingle,
                It.Is<GetTransactionByIdQuery>(q => q.TransactionId == transactionId),
                cancellationToken), 
                Times.Once);
        }

        [TestMethod]
        public async Task Post_invokes_ProcessRestCommandAsync_with_CreateWithContent_operation_and_CreateTransactionCommand_with_correct_transaction()
        {
            // Arrange
            var transaction = new Transaction
            {
                Id = Any.Int(),
                Description = Any.String(),
                Credit = Any.Decimal(),
                Debit = Any.Decimal()
            };
            var cancellationToken = new CancellationToken();
            
            _mediatorMock
                .Setup(m => m.ProcessRestCommandAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<CreateTransactionCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            // Act
            await _controller.Post(transaction, cancellationToken);

            // Assert
            _mediatorMock.Verify(m => m.ProcessRestCommandAsync(
                RestOperation.CreateWithContent,
                It.Is<CreateTransactionCommand>(c => c.Transaction == transaction),
                cancellationToken), 
                Times.Once);
        }

        [TestMethod]
        public async Task Put_invokes_ProcessRestCommandAsync_with_UpdateWithContent_operation_and_UpdateTransactionCommand_with_correct_id_and_transaction()
        {
            // Arrange
            var transactionId = Any.Int();
            var transaction = new Transaction
            {
                Id = Any.Int(),
                Description = Any.String(),
                Credit = Any.Decimal(),
                Debit = Any.Decimal()
            };
            var cancellationToken = new CancellationToken();
            
            _mediatorMock
                .Setup(m => m.ProcessRestCommandAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<UpdateTransactionCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            // Act
            await _controller.Put(transactionId, transaction, cancellationToken);

            // Assert
            _mediatorMock.Verify(m => m.ProcessRestCommandAsync(
                RestOperation.UpdateWithContent,
                It.Is<UpdateTransactionCommand>(c => c.TransactionId == transactionId && c.Transaction == transaction),
                cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task Delete_invokes_ProcessRestCommandAsync_with_Delete_operation_and_DeleteTransactionCommand_with_correct_id()
        {
            // Arrange
            var transactionId = Any.Int();
            var cancellationToken = new CancellationToken();

            _mediatorMock
                .Setup(m => m.ProcessRestCommandAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<DeleteTransactionCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            // Act
            await _controller.Delete(transactionId, cancellationToken);

            // Assert
            _mediatorMock.Verify(m => m.ProcessRestCommandAsync(
                RestOperation.Delete,
                It.Is<DeleteTransactionCommand>(c => c.TransactionId == transactionId),
                cancellationToken),
                Times.Once);
        }
    }
}
