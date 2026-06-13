using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnonymousData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using MockQueryable.Moq;
using Moq;
using MindedExample.Application.Category.Command;
using MindedExample.Application.Category.Validator;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using QM.Common.Testing;
using Minded.Extensions.Validation;

namespace MindedExample.Application.Category.UnitTests
{
    /// <summary>
    /// Unit tests for DeleteCategoryCommandValidator.
    /// Tests validation rules for deleting categories including existence checks.
    /// </summary>
    [TestClass]
    public class DeleteCategoryCommandValidatorTest
    {
        private const int TestTenantId = 7;

        private DeleteCategoryCommandValidator _sut;
        private Mock<IMindedExampleContext> _contextMock;
        private Mock<ICurrentUserAccessor> _currentUserAccessorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _contextMock = new Mock<IMindedExampleContext>();
            _currentUserAccessorMock = new Mock<ICurrentUserAccessor>();
            _currentUserAccessorMock.SetupGet(a => a.TenantId).Returns(TestTenantId);
            _sut = new DeleteCategoryCommandValidator(_contextMock.Object, _currentUserAccessorMock.Object);
        }

        /// <summary>
        /// Verifies that validation succeeds when category exists.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenCategoryExists()
        {
            var categoryId = Any.Int();
            var command = new DeleteCategoryCommand(categoryId);

            var categories = new List<MindedExample.Domain.Category>
            {
                new MindedExample.Domain.Category { Id = categoryId, User = new MindedExample.Domain.User { TenantId = TestTenantId } }
            };
            Mock<DbSet<MindedExample.Domain.Category>> mockDbSet = categories.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Categories).Returns(mockDbSet.Object);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation fails when category does not exist.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenCategoryDoesNotExist()
        {
            var categoryId = Any.Int();
            var command = new DeleteCategoryCommand(categoryId);

            var categories = new List<MindedExample.Domain.Category>();
            Mock<DbSet<MindedExample.Domain.Category>> mockDbSet = categories.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Categories).Returns(mockDbSet.Object);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.CategoryId) &&
                e.Severity == Severity.Error &&
                e.Message == "Category with ID {0} not found")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validator queries the database with correct category ID.
        /// </summary>
        [TestMethod]
        public async Task Validation_QueriesDatabaseWithCorrectCategoryId()
        {
            var categoryId = 42;
            var command = new DeleteCategoryCommand(categoryId);

            var categories = new List<MindedExample.Domain.Category>
            {
                new MindedExample.Domain.Category { Id = categoryId, User = new MindedExample.Domain.User { TenantId = TestTenantId } },
                new MindedExample.Domain.Category { Id = 99, User = new MindedExample.Domain.User { TenantId = TestTenantId } }
            };
            Mock<DbSet<MindedExample.Domain.Category>> mockDbSet = categories.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Categories).Returns(mockDbSet.Object);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }
    }
}

