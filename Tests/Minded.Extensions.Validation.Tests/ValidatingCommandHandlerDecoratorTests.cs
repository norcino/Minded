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
    /// Unit tests for ValidatingCommandHandlerDecorator (non-generic version).
    /// Tests validation behavior, delegation, and outcome entry merging.
    /// </summary>
    [TestClass]
    public class ValidatingCommandHandlerDecoratorTests
    {
        private Mock<ICommandHandler<TestValidatedCommand>> _mockInnerHandler;
        private Mock<ICommandValidator<TestValidatedCommand>> _mockValidator;
        private Mock<ILogger<ValidatingCommandHandlerDecorator<TestValidatedCommand>>> _mockLogger;
        private ValidatingCommandHandlerDecorator<TestValidatedCommand> _sut;

        [TestInitialize]
        public void Setup()
        {
            _mockInnerHandler = new Mock<ICommandHandler<TestValidatedCommand>>();
            _mockValidator = new Mock<ICommandValidator<TestValidatedCommand>>();
            _mockLogger = new Mock<ILogger<ValidatingCommandHandlerDecorator<TestValidatedCommand>>>();
            _sut = new ValidatingCommandHandlerDecorator<TestValidatedCommand>(
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
            var command = new TestNonValidatedCommand();
            var expectedResponse = new CommandResponse { Successful = true };
            var mockHandler = new Mock<ICommandHandler<TestNonValidatedCommand>>();
            var mockValidator = new Mock<ICommandValidator<TestNonValidatedCommand>>();
            var mockLogger = new Mock<ILogger<ValidatingCommandHandlerDecorator<TestNonValidatedCommand>>>();
            var sut = new ValidatingCommandHandlerDecorator<TestNonValidatedCommand>(
                mockHandler.Object,
                mockLogger.Object,
                mockValidator.Object);
            mockHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            ICommandResponse result = await sut.HandleAsync(command);

            result.Should().Be(expectedResponse);
            mockValidator.Verify(v => v.ValidateAsync(It.IsAny<TestNonValidatedCommand>()), Times.Never);
            mockHandler.Verify(h => h.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that HandleAsync returns early with validation errors when validation fails.
        /// Verifies inner handler is not called on validation failure.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenValidationFails_ReturnsEarlyWithErrors()
        {
            var command = new TestValidatedCommand();
            var validationResult = new Mock<IValidationResult>();
            var outcomeEntry = new OutcomeEntry(Any.String(), Any.String());
            validationResult.Setup(v => v.IsValid).Returns(false);
            validationResult.Setup(v => v.OutcomeEntries).Returns(new List<IOutcomeEntry> { outcomeEntry });
            _mockValidator.Setup(v => v.ValidateAsync(command))
                .ReturnsAsync(validationResult.Object);

            ICommandResponse result = await _sut.HandleAsync(command);

            result.Successful.Should().BeFalse();
            result.OutcomeEntries.Should().ContainSingle();
            result.OutcomeEntries[0].Should().Be(outcomeEntry);
            _mockInnerHandler.Verify(h => h.HandleAsync(It.IsAny<TestValidatedCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Tests that HandleAsync calls inner handler and merges outcome entries when validation succeeds.
        /// Verifies validation entries are added to successful response.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenValidationSucceeds_CallsInnerHandlerAndMergesEntries()
        {
            var command = new TestValidatedCommand();
            var validationResult = new Mock<IValidationResult>();
            var validationEntry = new OutcomeEntry(Any.String(), Any.String());
            validationResult.Setup(v => v.IsValid).Returns(true);
            validationResult.Setup(v => v.OutcomeEntries).Returns(new List<IOutcomeEntry> { validationEntry });
            _mockValidator.Setup(v => v.ValidateAsync(command))
                .ReturnsAsync(validationResult.Object);
            var handlerResponse = new CommandResponse { Successful = true };
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(handlerResponse);

            ICommandResponse result = await _sut.HandleAsync(command);

            result.Successful.Should().BeTrue();
            result.OutcomeEntries.Should().Contain(validationEntry);
            _mockInnerHandler.Verify(h => h.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that HandleAsync initializes OutcomeEntries when null.
        /// Verifies null collection is handled gracefully.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_WhenOutcomeEntriesNull_InitializesCollection()
        {
            var command = new TestValidatedCommand();
            var validationResult = new Mock<IValidationResult>();
            var validationEntry = new OutcomeEntry(Any.String(), Any.String());
            validationResult.Setup(v => v.IsValid).Returns(true);
            validationResult.Setup(v => v.OutcomeEntries).Returns(new List<IOutcomeEntry> { validationEntry });
            _mockValidator.Setup(v => v.ValidateAsync(command))
                .ReturnsAsync(validationResult.Object);
            var handlerResponse = new CommandResponse { Successful = true, OutcomeEntries = null };
            _mockInnerHandler.Setup(h => h.HandleAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(handlerResponse);

            ICommandResponse result = await _sut.HandleAsync(command);

            result.OutcomeEntries.Should().NotBeNull();
            result.OutcomeEntries.Should().Contain(validationEntry);
        }
    }

    [ValidateCommand]
    public class TestValidatedCommand : ICommand
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }

    public class TestNonValidatedCommand : ICommand
    {
        public Guid TraceId { get; set; } = Any.Guid();
    }
}

