using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using Moq;
using MindedExample.Application.Category.Command;
using MindedExample.Application.Category.Validator;
using Minded.Framework.Mediator;
using FluentAssertions;

namespace MindedExample.Application.Category.UnitTests
{
    /// <summary>
    /// Unit tests for UpdateCategoryCommandValidator.
    /// Tests validation rules for updating categories including existence checks and ID matching.
    /// </summary>
    [TestClass]
    public class UpdateCategoryCommandValidatorTest
    {
        private UpdateCategoryCommandValidator _sut;
        private Mock<IValidator<MindedExample.Domain.Category>> _categoryValidatorMock;
        private Mock<IMediator> _mediatorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _categoryValidatorMock = new Mock<IValidator<MindedExample.Domain.Category>>();
            _mediatorMock = new Mock<IMediator>();
            _sut = new UpdateCategoryCommandValidator(_categoryValidatorMock.Object, _mediatorMock.Object);

            _categoryValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<MindedExample.Domain.Category>()))
                .ReturnsAsync(new ValidationResult());
        }

        /// <summary>
        /// Verifies that validation succeeds when category exists and is valid for update.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenCategoryExistsAndIsValid()
        {
            MindedExample.Domain.Category category = Builder<MindedExample.Domain.Category>.New()
                .Build(c =>
                {
                    c.Id = 5;
                    c.Name = "Updated Category";
                });
            var command = new UpdateCategoryCommand(5, category);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<MindedExample.Application.Category.Query.ExistsCategoryInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation fails when command ID does not match category entity ID.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenCommandIdDoesNotMatchCategoryId()
        {
            MindedExample.Domain.Category category = Builder<MindedExample.Domain.Category>.New()
                .Build(c =>
                {
                    c.Id = 10;
                    c.Name = "Test Category";
                });
            var command = new UpdateCategoryCommand(5, category);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.CategoryId) &&
                e.Severity == Severity.Error &&
                e.Message == "Category ID in command does not match Category entity ID")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when category in command is null.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenCategoryIsNull()
        {
            var command = new UpdateCategoryCommand(5, null);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.Category) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation fails when category does not exist.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenCategoryDoesNotExist()
        {
            MindedExample.Domain.Category category = Builder<MindedExample.Domain.Category>.New()
                .Build(c =>
                {
                    c.Id = 999;
                    c.Name = "Non-existent Category";
                });
            var command = new UpdateCategoryCommand(999, category);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<MindedExample.Application.Category.Query.ExistsCategoryInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.CategoryId) &&
                e.Severity == Severity.Error &&
                e.Message == "Category with ID {0} not found")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validation merges results from category validator.
        /// </summary>
        [TestMethod]
        public async Task Validation_MergesResultsFromCategoryValidator()
        {
            MindedExample.Domain.Category category = Builder<MindedExample.Domain.Category>.New()
                .Build(c =>
                {
                    c.Id = 5;
                    c.Name = "";
                });
            var command = new UpdateCategoryCommand(5, category);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<MindedExample.Application.Category.Query.ExistsCategoryInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var categoryValidationResult = new ValidationResult();
            categoryValidationResult.OutcomeEntries.Add(
                new OutcomeEntry(nameof(category.Name), "{0} is mandatory"));
            _categoryValidatorMock.Setup(v => v.ValidateAsync(category))
                .ReturnsAsync(categoryValidationResult);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(category.Name) &&
                e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }
    }
}

