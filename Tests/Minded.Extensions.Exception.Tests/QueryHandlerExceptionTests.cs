using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using System;

namespace Minded.Extensions.Exception.Tests
{
    /// <summary>
    /// Unit tests for QueryHandlerException class.
    /// Tests constructors, properties, and exception behavior for query handler exceptions.
    /// </summary>
    [TestClass]
    public class QueryHandlerExceptionTests
    {
        /// <summary>
        /// Tests constructor with query, message, and error code.
        /// </summary>
        [TestMethod]
        public void Constructor_WithQueryMessageAndErrorCode_SetsPropertiesCorrectly()
        {
            var query = new TestQuery();
            var message = Any.String();
            var errorCode = Any.String();

            var exception = new QueryHandlerException<TestQuery, int>(query, message, errorCode);

            exception.Query.Should().BeSameAs(query);
            exception.Message.Should().Be(message);
            exception.ErrorCode.Should().Be(errorCode);
            exception.InnerException.Should().BeNull();
        }

        /// <summary>
        /// Tests constructor with query, message, error code, and inner exception.
        /// </summary>
        [TestMethod]
        public void Constructor_WithQueryMessageErrorCodeAndInnerException_SetsPropertiesCorrectly()
        {
            var query = new TestQuery();
            var message = Any.String();
            var errorCode = Any.String();
            var innerException = new InvalidOperationException(Any.String());

            var exception = new QueryHandlerException<TestQuery, int>(query, message, errorCode, innerException);

            exception.Query.Should().BeSameAs(query);
            exception.Message.Should().Be(message);
            exception.ErrorCode.Should().Be(errorCode);
            exception.InnerException.Should().BeSameAs(innerException);
        }

        /// <summary>
        /// Tests constructor with query and message (without error code).
        /// </summary>
        [TestMethod]
        public void Constructor_WithQueryAndMessage_SetsPropertiesAndDefaultErrorCode()
        {
            var query = new TestQuery();
            var message = Any.String();

            var exception = new QueryHandlerException<TestQuery, int>(query, message);

            exception.Query.Should().BeSameAs(query);
            exception.Message.Should().Be(message);
            exception.ErrorCode.Should().Be(GenericErrorCodes.Unknown);
            exception.InnerException.Should().BeNull();
        }

        /// <summary>
        /// Tests constructor with query, message, and inner exception (without error code).
        /// </summary>
        [TestMethod]
        public void Constructor_WithQueryMessageAndInnerException_SetsPropertiesAndDefaultErrorCode()
        {
            var query = new TestQuery();
            var message = Any.String();
            var innerException = new InvalidOperationException(Any.String());

            var exception = new QueryHandlerException<TestQuery, int>(query, message, innerException);

            exception.Query.Should().BeSameAs(query);
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
            var query = new TestQuery();
            var exception = new QueryHandlerException<TestQuery, int>(query, Any.String());
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
            var query = new TestQuery();
            var message = Any.String();
            var errorCode = Any.String();

            Action act = () => throw new QueryHandlerException<TestQuery, int>(query, message, errorCode);

            act.Should().Throw<QueryHandlerException<TestQuery, int>>()
                .Which.Query.Should().BeSameAs(query);
        }

        #region Test Helper Classes

        public class TestQuery : IQuery<int>
        {
            public Guid TraceId { get; set; } = Any.Guid();
        }

        #endregion
    }
}

