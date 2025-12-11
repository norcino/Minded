using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Decorator;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Minded.Framework.Decorator.Tests
{
    /// <summary>
    /// Unit tests for CommandHandlerDecoratorBase and QueryHandlerDecoratorBase classes.
    /// Tests constructor and property access to ensure correct decorator pattern implementation.
    /// </summary>
    [TestClass]
    public class DecoratorBaseTests
    {
        #region CommandHandlerDecoratorBase (Non-Generic) Tests

        /// <summary>
        /// Tests that CommandHandlerDecoratorBase stores the decorated handler correctly.
        /// </summary>
        [TestMethod]
        public void CommandHandlerDecoratorBase_Constructor_StoresDecoratedHandler()
        {
            var mockHandler = new Mock<ICommandHandler<TestCommand>>();

            var decorator = new TestCommandDecorator(mockHandler.Object);

            decorator.InnerCommandHandler.Should().BeSameAs(mockHandler.Object);
        }

        /// <summary>
        /// Tests that InnerCommandHandler property returns the decorated handler.
        /// </summary>
        [TestMethod]
        public void CommandHandlerDecoratorBase_InnerCommandHandler_ReturnsDecoratedHandler()
        {
            var mockHandler = new Mock<ICommandHandler<TestCommand>>();
            var decorator = new TestCommandDecorator(mockHandler.Object);

            ICommandHandler<TestCommand> innerHandler = decorator.InnerCommandHandler;

            innerHandler.Should().BeSameAs(mockHandler.Object);
        }

        #endregion

        #region CommandHandlerDecoratorBase<TCommand, TResult> (Generic) Tests

        /// <summary>
        /// Tests that generic CommandHandlerDecoratorBase stores the decorated handler correctly.
        /// </summary>
        [TestMethod]
        public void CommandHandlerDecoratorBaseGeneric_Constructor_StoresDecoratedHandler()
        {
            var mockHandler = new Mock<ICommandHandler<TestCommandWithResult, string>>();

            var decorator = new TestCommandWithResultDecorator(mockHandler.Object);

            decorator.InnerCommandHandler.Should().BeSameAs(mockHandler.Object);
        }

        /// <summary>
        /// Tests that InnerCommandHandler property returns the decorated handler for generic decorator.
        /// </summary>
        [TestMethod]
        public void CommandHandlerDecoratorBaseGeneric_InnerCommandHandler_ReturnsDecoratedHandler()
        {
            var mockHandler = new Mock<ICommandHandler<TestCommandWithResult, string>>();
            var decorator = new TestCommandWithResultDecorator(mockHandler.Object);

            ICommandHandler<TestCommandWithResult, string> innerHandler = decorator.InnerCommandHandler;

            innerHandler.Should().BeSameAs(mockHandler.Object);
        }

        #endregion

        #region QueryHandlerDecoratorBase Tests

        /// <summary>
        /// Tests that QueryHandlerDecoratorBase stores the decorated handler correctly.
        /// </summary>
        [TestMethod]
        public void QueryHandlerDecoratorBase_Constructor_StoresDecoratedHandler()
        {
            var mockHandler = new Mock<IQueryHandler<TestQuery, int>>();

            var decorator = new TestQueryDecorator(mockHandler.Object);

            decorator.InnerQueryHandler.Should().BeSameAs(mockHandler.Object);
        }

        /// <summary>
        /// Tests that InnerQueryHandler property returns the decorated handler.
        /// </summary>
        [TestMethod]
        public void QueryHandlerDecoratorBase_InnerQueryHandler_ReturnsDecoratedHandler()
        {
            var mockHandler = new Mock<IQueryHandler<TestQuery, int>>();
            var decorator = new TestQueryDecorator(mockHandler.Object);

            IQueryHandler<TestQuery, int> innerHandler = decorator.InnerQueryHandler;

            innerHandler.Should().BeSameAs(mockHandler.Object);
        }

        #endregion

        #region Test Helper Classes

        public class TestCommand : ICommand
        {
            public Guid TraceId { get; set; } = Any.Guid();
        }

        public class TestCommandWithResult : ICommand<string>
        {
            public Guid TraceId { get; set; } = Any.Guid();
        }

        public class TestQuery : IQuery<int>
        {
            public Guid TraceId { get; set; } = Any.Guid();
        }

        public class TestCommandDecorator : CommandHandlerDecoratorBase<TestCommand>, ICommandHandler<TestCommand>
        {
            public TestCommandDecorator(ICommandHandler<TestCommand> commandHandler) : base(commandHandler) { }

            public Task<ICommandResponse> HandleAsync(TestCommand command, CancellationToken cancellationToken = default)
            {
                return DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
            }
        }

        public class TestCommandWithResultDecorator : CommandHandlerDecoratorBase<TestCommandWithResult, string>, ICommandHandler<TestCommandWithResult, string>
        {
            public TestCommandWithResultDecorator(ICommandHandler<TestCommandWithResult, string> commandHandler) : base(commandHandler) { }

            public Task<ICommandResponse<string>> HandleAsync(TestCommandWithResult command, CancellationToken cancellationToken = default)
            {
                return DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
            }
        }

        public class TestQueryDecorator : QueryHandlerDecoratorBase<TestQuery, int>, IQueryHandler<TestQuery, int>
        {
            public TestQueryDecorator(IQueryHandler<TestQuery, int> queryHandler) : base(queryHandler) { }

            public Task<int> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
            {
                return DecoratedQueryHandler.HandleAsync(query, cancellationToken);
            }
        }

        #endregion
    }
}

