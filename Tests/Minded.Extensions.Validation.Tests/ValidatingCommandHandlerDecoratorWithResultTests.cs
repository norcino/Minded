using AnonymousData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Validation;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Minded.Extensions.Validation.Tests
{
    /// <summary>
    /// Unit tests for ValidatingCommandHandlerDecorator (generic version with TResult).
    /// Tests validation behavior, delegation, and outcome entry merging.
    /// </summary>
    [TestClass]
    public class ValidatingCommandHandlerDecoratorWithResultTests
    {
        private Mock<ICommandHandler<TestValidatedCommandWithResult, string>> _mockInnerHandler;
        private Mock<ICommandValidator<TestValidatedCommandWithResult>> _mockValidator;
        private Mock<ILogger<ValidatingCommandHandlerDecorator<TestValidatedCommandWithResult, string>>> _mockLogger;
        private ValidatingCommandHandlerDecorator<TestValidatedCommandWithResult, string> _sut;

        [TestInitialize]
        public void Setup()
        {
            _mockInnerHandler = new Mock<ICommandHandler<TestValidatedCommandWithResult, string>>();
            _mockValidator = new Mock<ICommandValidator<TestValidatedCommandWithResult>>();
            _mockLogger = new Mock<ILogger<ValidatingCommandHandlerDecorator<TestValidatedCommandWithResult, string>>>();
            _sut = new ValidatingCommandHandlerDecorator<TestValidatedCommandWithResult, string>(
                _mockInnerHandler.Object,
                _mockLogger.Object,
                _mockValidator.Object);
        }

        /// <summary>
        /// Tests that HandleAsync bypasses validation when command has no ValidateCommandAttribute.
        /// Verifies validator is not called for non-validated commands.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenCommandNotValidated_BypassesValidation()
        {
            var command = new TestNonValidatedCommandWithResult();
            var expectedResponse = new CommandResponse<int>(Any.Int());
            var mockHandler = new Mock<ICommandHandler<TestNonValidatedCommandWithResult, int>>();
            var mockValidator = new Mock<ICommandValidator<TestNonValidatedCommandWithResult>>();
            var mockLogger = new Mock<ILogger<ValidatingCommandHandlerDecorator<TestNonValidatedCommandWithResult, int>>>();
            var sut = new ValidatingCommandHandlerDecorator<TestNonValidatedCommandWithResult, int>(
                mockHandler.Object,
                mockLogger.Object,
                mockValidator.Object);
            mockHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            ICommandResponse<int> result = await sut.HandleAsync(command);

            result.Should().Be(expectedResponse);
            mockValidator.Verify(v => v.ValidateAsync(It.IsAny<TestNonValidatedCommandWithResult>()), Times.Never);
            mockHandler.Verify(h => h.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that HandleAsync returns early with validation errors when validation fails.
        /// Verifies inner handler is not called on validation failure.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenValidationFails_ReturnsEarlyWithErrors()
        {
            var command = new TestValidatedCommandWithResult();
            var validationResult = new Mock<IValidationResult>();
            var outcomeEntry = new OutcomeEntry(Any.String(), Any.String());
            validationResult.Setup(v => v.IsValid).Returns(false);
            validationResult.Setup(v => v.OutcomeEntries).Returns(new List<IOutcomeEntry> { outcomeEntry });
            _mockValidator.Setup(v => v.ValidateAsync(command))
                .ReturnsAsync(validationResult.Object);

            ICommandResponse<string> result = await _sut.HandleAsync(command);

            result.Successful.Should().BeFalse();
            result.OutcomeEntries.Should().ContainSingle();
            result.OutcomeEntries[0].Should().Be(outcomeEntry);
            _mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestValidatedCommandWithResult>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Tests that HandleAsync calls inner handler when validation succeeds.
        /// Verifies successful validation returns inner handler result directly.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenValidationSucceeds_CallsInnerHandler()
        {
            var command = new TestValidatedCommandWithResult();
            var validationResult = new Mock<IValidationResult>();
            validationResult.Setup(v => v.IsValid).Returns(true);
            validationResult.Setup(v => v.OutcomeEntries).Returns(new List<IOutcomeEntry>());
            _mockValidator.Setup(v => v.ValidateAsync(command))
                .ReturnsAsync(validationResult.Object);
            var expectedResult = Any.String();
            var handlerResponse = new CommandResponse<string>(expectedResult) { Successful = true };
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(handlerResponse);

            ICommandResponse<string> result = await _sut.HandleAsync(command);

            result.Successful.Should().BeTrue();
            result.Result.Should().Be(expectedResult);
            _mockInnerHandler.Verify(h => h.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [ValidateCommand]
    public class TestValidatedCommandWithResult : ICommand<string>
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }

    public class TestNonValidatedCommandWithResult : ICommand<int>
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }
}

