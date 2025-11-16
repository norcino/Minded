using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using System;

namespace Minded.Extensions.Exception.Tests
{
    /// <summary>
    /// Unit tests for CommandHandlerException class.
    /// Tests constructors, properties, and exception behavior for command handler exceptions.
    /// </summary>
    [TestClass]
    public class CommandHandlerExceptionTests
    {
        /// <summary>
        /// Tests constructor with command, message, and error code.
        /// </summary>
        [TestMethod]
        public void Constructor_WithCommandMessageAndErrorCode_SetsPropertiesCorrectly()
        {
            var command = new TestCommand();
            var message = Any.String();
            var errorCode = Any.String();

            var exception = new CommandHandlerException<TestCommand>(command, message, errorCode);

            exception.Command.Should().BeSameAs(command);
            exception.Message.Should().Be(message);
            exception.ErrorCode.Should().Be(errorCode);
            exception.InnerException.Should().BeNull();
        }

        /// <summary>
        /// Tests constructor with command, message, error code, and inner exception.
        /// </summary>
        [TestMethod]
        public void Constructor_WithCommandMessageErrorCodeAndInnerException_SetsPropertiesCorrectly()
        {
            var command = new TestCommand();
            var message = Any.String();
            var errorCode = Any.String();
            var innerException = new InvalidOperationException(Any.String());

            var exception = new CommandHandlerException<TestCommand>(command, message, errorCode, innerException);

            exception.Command.Should().BeSameAs(command);
            exception.Message.Should().Be(message);
            exception.ErrorCode.Should().Be(errorCode);
            exception.InnerException.Should().BeSameAs(innerException);
        }

        /// <summary>
        /// Tests constructor with command and message (without error code).
        /// </summary>
        [TestMethod]
        public void Constructor_WithCommandAndMessage_SetsPropertiesAndDefaultErrorCode()
        {
            var command = new TestCommand();
            var message = Any.String();

            var exception = new CommandHandlerException<TestCommand>(command, message);

            exception.Command.Should().BeSameAs(command);
            exception.Message.Should().Be(message);
            exception.ErrorCode.Should().Be(GenericErrorCodes.Unknown);
            exception.InnerException.Should().BeNull();
        }

        /// <summary>
        /// Tests constructor with command, message, and inner exception (without error code).
        /// </summary>
        [TestMethod]
        public void Constructor_WithCommandMessageAndInnerException_SetsPropertiesAndDefaultErrorCode()
        {
            var command = new TestCommand();
            var message = Any.String();
            var innerException = new InvalidOperationException(Any.String());

            var exception = new CommandHandlerException<TestCommand>(command, message, innerException);

            exception.Command.Should().BeSameAs(command);
            exception.Message.Should().Be(message);
            exception.ErrorCode.Should().Be(GenericErrorCodes.Unknown);
            exception.InnerException.Should().BeSameAs(innerException);
        }

        /// <summary>
        /// Tests that ErrorCode property can be set after construction.
        /// </summary>
        [TestMethod]
        public void ErrorCode_CanBeSetAfterConstruction()
        {
            var command = new TestCommand();
            var exception = new CommandHandlerException<TestCommand>(command, Any.String());
            var newErrorCode = Any.String();

            exception.ErrorCode = newErrorCode;

            exception.ErrorCode.Should().Be(newErrorCode);
        }

        /// <summary>
        /// Tests that exception can be thrown and caught.
        /// </summary>
        [TestMethod]
        public void Exception_CanBeThrownAndCaught()
        {
            var command = new TestCommand();
            var message = Any.String();
            var errorCode = Any.String();

            Action act = () => throw new CommandHandlerException<TestCommand>(command, message, errorCode);

            act.Should().Throw<CommandHandlerException<TestCommand>>()
                .Which.Command.Should().BeSameAs(command);
        }

        #region Test Helper Classes

        public class TestCommand : ICommand
        {
            public Guid TraceId { get; set; } = Any.Guid();
        }

        #endregion
    }
}

