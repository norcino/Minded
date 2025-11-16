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
using Service.Category.Command;
using Service.Category.Validator;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using QM.Common.Testing;

namespace Service.Category.Tests
{
    /// <summary>
    /// Unit tests for DeleteCategoryCommandValidator.
    /// Tests validation rules for deleting categories including existence checks.
    /// </summary>
    [TestClass]
    public class DeleteCategoryCommandValidatorTest
    {
        private DeleteCategoryCommandValidator _sut;
        private Mock<IMindedExampleContext> _contextMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _contextMock = new Mock<IMindedExampleContext>();
            _sut = new DeleteCategoryCommandValidator(_contextMock.Object);
        }

        /// <summary>
        /// Verifies that validation succeeds when category exists.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenCategoryExists()
        {
            var categoryId = Any.Int();
            var command = new DeleteCategoryCommand(categoryId);

            var categories = new List<Data.Entity.Category>
            {
                new Data.Entity.Category { Id = categoryId }
            };
            var mockDbSet = categories.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Categories).Returns(mockDbSet.Object);

            var result = await _sut.ValidateAsync(command);

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

            var categories = new List<Data.Entity.Category>();
            var mockDbSet = categories.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Categories).Returns(mockDbSet.Object);

            var result = await _sut.ValidateAsync(command);

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

            var categories = new List<Data.Entity.Category>
            {
                new Data.Entity.Category { Id = categoryId },
                new Data.Entity.Category { Id = 99 }
            };
            var mockDbSet = categories.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Categories).Returns(mockDbSet.Object);

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }
    }
}

