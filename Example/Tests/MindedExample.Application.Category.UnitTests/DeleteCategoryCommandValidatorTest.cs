using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnonymousData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.Mediator;
using Moq;
using MindedExample.Application.Category.Command;
using MindedExample.Application.Category.Query;
using MindedExample.Application.Category.Validator;
using FluentAssertions;

namespace MindedExample.Application.Category.UnitTests
{
    /// <summary>
    /// Unit tests for DeleteCategoryCommandValidator.
    /// Validates that the validator correctly dispatches ExistsCategoryInCurrentTenantQuery via IMediator
    /// and returns the appropriate validation errors when the category is not found.
    /// </summary>
    [TestClass]
    public class DeleteCategoryCommandValidatorTest
    {
        private DeleteCategoryCommandValidator _sut;
        private Mock<IMediator> _mediatorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _mediatorMock = new Mock<IMediator>();
            _sut = new DeleteCategoryCommandValidator(_mediatorMock.Object);
        }

        /// <summary>
        /// Verifies that validation succeeds when the category exists.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenCategoryExists()
        {
            var categoryId = Any.Int();
            var command = new DeleteCategoryCommand(categoryId);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsCategoryInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation fails with a 404-style error when the category does not exist.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenCategoryDoesNotExist()
        {
            var categoryId = Any.Int();
            var command = new DeleteCategoryCommand(categoryId);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsCategoryInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
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
        /// Verifies that the validator dispatches ExistsCategoryInCurrentTenantQuery with the correct category ID.
        /// </summary>
        [TestMethod]
        public async Task Validation_DispatchesQueryWithCorrectCategoryId()
        {
            var categoryId = 42;
            var command = new DeleteCategoryCommand(categoryId);

            _mediatorMock.Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsCategoryInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await _sut.ValidateAsync(command);

            _mediatorMock.Verify(m => m.ProcessQueryAsync(
                It.Is<ExistsCategoryInCurrentTenantQuery>(q => q.CategoryId == categoryId),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

