using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Minded.Framework.Mediator.Tests
{
    /// <summary>
    /// Unit tests for the Mediator class.
    /// Tests all methods to ensure correct handler resolution and execution.
    /// </summary>
    [TestClass]
    public class MediatorTests
    {
        private Mock<IServiceProvider> _mockServiceProvider;
        private Mediator _sut;

        [TestInitialize]
        public void Setup()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _sut = new Mediator(_mockServiceProvider.Object);
        }

        #region Constructor Tests

        /// <summary>
        /// Tests that the constructor accepts IServiceProvider.
        /// Verifies that the mediator can be instantiated.
        /// </summary>
        [TestMethod]
        public void Constructor_WithServiceProvider_CreatesInstance()
        {
            var mediator = new Mediator(_mockServiceProvider.Object);

            mediator.Should().NotBeNull();
            mediator.Should().BeAssignableTo<IMediator>();
        }

        #endregion

        #region ProcessQueryAsync Tests

        /// <summary>
        /// Tests ProcessQueryAsync with a valid query and handler.
        /// Verifies that the handler is resolved and executed correctly.
        /// </summary>
        [TestMethod]
        public async Task ProcessQueryAsync_WithValidQuery_ReturnsResult()
        {
            var query = new TestQuery();
            var expectedResult = Any.Int();
            var mockHandler = new Mock<IQueryHandler<TestQuery, int>>();
            mockHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(expectedResult);

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IQueryHandler<TestQuery, int>)))
                                .Returns(mockHandler.Object);

            var result = await _sut.ProcessQueryAsync(query);

            result.Should().Be(expectedResult);
            mockHandler.Verify(h => h.HandleAsync(query, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests ProcessQueryAsync when handler is not found.
        /// Verifies that InvalidOperationException is thrown.
        /// </summary>
        [TestMethod]
        public async Task ProcessQueryAsync_WhenHandlerNotFound_ThrowsInvalidOperationException()
        {
            var query = new TestQuery();
            _mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
                                .Returns(null);

            Func<Task> act = async () => await _sut.ProcessQueryAsync(query);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Unable to retrieve the handler for query:*");
        }

        /// <summary>
        /// Tests ProcessQueryAsync with CancellationToken.
        /// Verifies that the cancellation token is passed to the handler.
        /// </summary>
        [TestMethod]
        public async Task ProcessQueryAsync_WithCancellationToken_PassesTokenToHandler()
        {
            var query = new TestQuery();
            var cancellationToken = new CancellationToken();
            var mockHandler = new Mock<IQueryHandler<TestQuery, int>>();
            mockHandler.Setup(h => h.HandleAsync(query, cancellationToken))
                       .ReturnsAsync(Any.Int());

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IQueryHandler<TestQuery, int>)))
                                .Returns(mockHandler.Object);

            await _sut.ProcessQueryAsync(query, cancellationToken);

            mockHandler.Verify(h => h.HandleAsync(query, cancellationToken), Times.Once);
        }

        #endregion

        #region ProcessCommandAsync (Non-Generic) Tests

        /// <summary>
        /// Tests ProcessCommandAsync with a valid command and handler.
        /// Verifies that the handler is resolved and executed correctly.
        /// </summary>
        [TestMethod]
        public async Task ProcessCommandAsync_WithValidCommand_ReturnsResponse()
        {
            var command = new TestCommand();
            var expectedResponse = new CommandResponse { Successful = true };
            var mockHandler = new Mock<ICommandHandler<TestCommand>>();
            mockHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(expectedResponse);

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ICommandHandler<TestCommand>)))
                                .Returns(mockHandler.Object);

            var result = await _sut.ProcessCommandAsync(command);

            result.Should().Be(expectedResponse);
            result.Successful.Should().BeTrue();
            mockHandler.Verify(h => h.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests ProcessCommandAsync when handler is not found.
        /// Verifies that InvalidOperationException is thrown.
        /// </summary>
        [TestMethod]
        public async Task ProcessCommandAsync_WhenHandlerNotFound_ThrowsInvalidOperationException()
        {
            var command = new TestCommand();
            _mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
                                .Returns(null);

            Func<Task> act = async () => await _sut.ProcessCommandAsync(command);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Unable to retrieve the handler for command:*");
        }

        #endregion

        #region ProcessCommandAsync<TResult> (Generic) Tests

        /// <summary>
        /// Tests ProcessCommandAsync<TResult> with a valid command and handler.
        /// Verifies that the handler is resolved and executed correctly.
        /// </summary>
        [TestMethod]
        public async Task ProcessCommandAsyncGeneric_WithValidCommand_ReturnsResponse()
        {
            var command = new TestCommandWithResult();
            var expectedResult = Any.String();
            var expectedResponse = new CommandResponse<string>(expectedResult);
            var mockHandler = new Mock<ICommandHandler<TestCommandWithResult, string>>();
            mockHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(expectedResponse);

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ICommandHandler<TestCommandWithResult, string>)))
                                .Returns(mockHandler.Object);

            var result = await _sut.ProcessCommandAsync(command);

            result.Should().Be(expectedResponse);
            result.Result.Should().Be(expectedResult);
            mockHandler.Verify(h => h.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests ProcessCommandAsync<TResult> when handler is not found.
        /// Verifies that InvalidOperationException is thrown.
        /// </summary>
        [TestMethod]
        public async Task ProcessCommandAsyncGeneric_WhenHandlerNotFound_ThrowsInvalidOperationException()
        {
            var command = new TestCommandWithResult();
            _mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
                                .Returns(null);

            Func<Task> act = async () => await _sut.ProcessCommandAsync(command);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Unable to retrieve the handler for command:*");
        }

        /// <summary>
        /// Tests ProcessCommandAsync<TResult> when handler returns null.
        /// Verifies that a failed response with outcome entry is returned.
        /// </summary>
        [TestMethod]
        public async Task ProcessCommandAsyncGeneric_WhenHandlerReturnsNull_ReturnsFailedResponse()
        {
            var command = new TestCommandWithResult();
            var mockHandler = new Mock<ICommandHandler<TestCommandWithResult, string>>();
            mockHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((ICommandResponse<string>)null);

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ICommandHandler<TestCommandWithResult, string>)))
                                .Returns(mockHandler.Object);

            var result = await _sut.ProcessCommandAsync(command);

            result.Should().NotBeNull();
            result.Successful.Should().BeFalse();
            result.OutcomeEntries.Should().HaveCount(1);
            result.OutcomeEntries[0].Message.Should().Be("The handler returned a null result");
        }

        #endregion

        #region Test Helper Classes

        /// <summary>
        /// Test query for unit testing.
        /// </summary>
        public class TestQuery : IQuery<int>
        {
            public Guid TraceId { get; set; } = Guid.NewGuid();
        }

        /// <summary>
        /// Test command for unit testing.
        /// </summary>
        public class TestCommand : ICommand
        {
            public Guid TraceId { get; set; } = Guid.NewGuid();
        }

        /// <summary>
        /// Test command with result for unit testing.
        /// </summary>
        public class TestCommandWithResult : ICommand<string>
        {
            public Guid TraceId { get; set; } = Guid.NewGuid();
        }

        #endregion
    }
}


