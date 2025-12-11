using AnonymousData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Minded.Extensions.Validation.Tests
{
    /// <summary>
    /// Unit tests for ValidatingQueryHandlerDecorator.
    /// Tests validation behavior, delegation, outcome entry merging, and type constraints.
    /// </summary>
    [TestClass]
    public class ValidatingQueryHandlerDecoratorTests
    {
        private Mock<IQueryHandler<TestValidatedQuery, IQueryResponse<int>>> _mockInnerHandler;
        private Mock<IQueryValidator<TestValidatedQuery, IQueryResponse<int>>> _mockValidator;
        private Mock<ILogger<ValidatingQueryHandlerDecorator<TestValidatedQuery, IQueryResponse<int>>>> _mockLogger;
        private ValidatingQueryHandlerDecorator<TestValidatedQuery, IQueryResponse<int>> _sut;

        [TestInitialize]
        public void Setup()
        {
            _mockInnerHandler = new Mock<IQueryHandler<TestValidatedQuery, IQueryResponse<int>>>();
            _mockValidator = new Mock<IQueryValidator<TestValidatedQuery, IQueryResponse<int>>>();
            _mockLogger = new Mock<ILogger<ValidatingQueryHandlerDecorator<TestValidatedQuery, IQueryResponse<int>>>>();
            _sut = new ValidatingQueryHandlerDecorator<TestValidatedQuery, IQueryResponse<int>>(
                _mockInnerHandler.Object,
                _mockLogger.Object,
                _mockValidator.Object);
        }

        /// <summary>
        /// Tests that HandleAsync bypasses validation when query has no ValidateQueryAttribute.
        /// Verifies validator is not called for non-validated queries.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenQueryNotValidated_BypassesValidation()
        {
            var query = new TestNonValidatedQuery();
            var expectedResult = Any.Int();
            var mockHandler = new Mock<IQueryHandler<TestNonValidatedQuery, int>>();
            var mockValidator = new Mock<IQueryValidator<TestNonValidatedQuery, int>>();
            var mockLogger = new Mock<ILogger<ValidatingQueryHandlerDecorator<TestNonValidatedQuery, int>>>();
            var sut = new ValidatingQueryHandlerDecorator<TestNonValidatedQuery, int>(
                mockHandler.Object,
                mockLogger.Object,
                mockValidator.Object);
            mockHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var result = await sut.HandleAsync(query);

            result.Should().Be(expectedResult);
            mockValidator.Verify(v => v.ValidateAsync(It.IsAny<TestNonValidatedQuery>()), Times.Never);
            mockHandler.Verify(h => h.HandleAsync(query, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that HandleAsync throws InvalidOperationException when query doesn't return IQueryResponse.
        /// Verifies type constraint is enforced.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenQueryNotReturningIQueryResponse_ThrowsInvalidOperationException()
        {
            var query = new TestInvalidValidatedQuery();
            var mockHandler = new Mock<IQueryHandler<TestInvalidValidatedQuery, int>>();
            var mockValidator = new Mock<IQueryValidator<TestInvalidValidatedQuery, int>>();
            var mockLogger = new Mock<ILogger<ValidatingQueryHandlerDecorator<TestInvalidValidatedQuery, int>>>();
            var sut = new ValidatingQueryHandlerDecorator<TestInvalidValidatedQuery, int>(
                mockHandler.Object,
                mockLogger.Object,
                mockValidator.Object);

            Func<Task> act = async () => await sut.HandleAsync(query);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*IQueryResponse*");
        }

        /// <summary>
        /// Tests that HandleAsync returns early with validation errors when validation fails.
        /// Verifies inner handler is not called on validation failure.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenValidationFails_ReturnsEarlyWithErrors()
        {
            var query = new TestValidatedQuery();
            var validationResult = new Mock<IValidationResult>();
            var outcomeEntry = new OutcomeEntry(Any.String(), Any.String());
            validationResult.Setup(v => v.IsValid).Returns(false);
            validationResult.Setup(v => v.OutcomeEntries).Returns(new List<IOutcomeEntry> { outcomeEntry });
            _mockValidator.Setup(v => v.ValidateAsync(query))
                .ReturnsAsync(validationResult.Object);

            IQueryResponse<int> result = await _sut.HandleAsync(query);

            result.Successful.Should().BeFalse();
            result.OutcomeEntries.Should().ContainSingle();
            result.OutcomeEntries[0].Should().Be(outcomeEntry);
            _mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestValidatedQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Tests that HandleAsync calls inner handler and merges outcome entries when validation succeeds.
        /// Verifies validation entries are added to successful response.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenValidationSucceeds_CallsInnerHandlerAndMergesEntries()
        {
            var query = new TestValidatedQuery();
            var validationResult = new Mock<IValidationResult>();
            var validationEntry = new OutcomeEntry(Any.String(), Any.String());
            validationResult.Setup(v => v.IsValid).Returns(true);
            validationResult.Setup(v => v.OutcomeEntries).Returns(new List<IOutcomeEntry> { validationEntry });
            _mockValidator.Setup(v => v.ValidateAsync(query))
                .ReturnsAsync(validationResult.Object);
            var handlerResponse = new QueryResponse<int>(Any.Int());
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync(handlerResponse);

            IQueryResponse<int> result = await _sut.HandleAsync(query);

            result.Successful.Should().BeTrue();
            result.OutcomeEntries.Should().Contain(validationEntry);
            _mockInnerHandler.Verify(h => h.HandleAsync(query, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that HandleAsync initializes OutcomeEntries when null.
        /// Verifies null collection is handled gracefully.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenOutcomeEntriesNull_InitializesCollection()
        {
            var query = new TestValidatedQuery();
            var validationResult = new Mock<IValidationResult>();
            var validationEntry = new OutcomeEntry(Any.String(), Any.String());
            validationResult.Setup(v => v.IsValid).Returns(true);
            validationResult.Setup(v => v.OutcomeEntries).Returns(new List<IOutcomeEntry> { validationEntry });
            _mockValidator.Setup(v => v.ValidateAsync(query))
                .ReturnsAsync(validationResult.Object);
            var handlerResponse = new QueryResponse<int>(Any.Int()) { OutcomeEntries = null };
            _mockInnerHandler.Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync(handlerResponse);

            IQueryResponse<int> result = await _sut.HandleAsync(query);

            result.OutcomeEntries.Should().NotBeNull();
            result.OutcomeEntries.Should().Contain(validationEntry);
        }
    }

    [ValidateQuery]
    public class TestValidatedQuery : IQuery<IQueryResponse<int>>
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }

    public class TestNonValidatedQuery : IQuery<int>
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }

    [ValidateQuery]
    public class TestInvalidValidatedQuery : IQuery<int>
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }
}

