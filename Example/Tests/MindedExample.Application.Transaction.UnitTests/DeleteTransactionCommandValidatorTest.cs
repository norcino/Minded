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
using MindedExample.Application.Transaction.Command;
using MindedExample.Application.Transaction.Validator;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using QM.Common.Testing;
using Minded.Extensions.Validation;

namespace MindedExample.Application.Transaction.UnitTests
{
    /// <summary>
    /// Unit tests for DeleteTransactionCommandValidator.
    /// Tests validation rules for deleting transactions including existence checks.
    /// </summary>
    [TestClass]
    public class DeleteTransactionCommandValidatorTest
    {
        private DeleteTransactionCommandValidator _sut;
        private Mock<IMindedExampleContext> _contextMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _contextMock = new Mock<IMindedExampleContext>();
            _sut = new DeleteTransactionCommandValidator(_contextMock.Object);
        }

        /// <summary>
        /// Verifies that validation succeeds when transaction exists.
        /// </summary>
        [TestMethod]
        public async Task Validation_Succeeds_WhenTransactionExists()
        {
            var transactionId = Any.Int();
            var command = new DeleteTransactionCommand(transactionId);

            var transactions = new List<MindedExample.Domain.Transaction>
            {
                new MindedExample.Domain.Transaction { Id = transactionId }
            };
            Mock<DbSet<MindedExample.Domain.Transaction>> mockDbSet = transactions.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Transactions).Returns(mockDbSet.Object);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
            result.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation fails when transaction does not exist.
        /// </summary>
        [TestMethod]
        public async Task Validation_Fails_WhenTransactionDoesNotExist()
        {
            var transactionId = Any.Int();
            var command = new DeleteTransactionCommand(transactionId);

            var transactions = new List<MindedExample.Domain.Transaction>();
            Mock<DbSet<MindedExample.Domain.Transaction>> mockDbSet = transactions.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Transactions).Returns(mockDbSet.Object);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.TransactionId) &&
                e.Severity == Severity.Error &&
                e.Message == "Transaction with ID {0} not found")
                .Should().BeTrue();
        }

        /// <summary>
        /// Verifies that validator queries the database with correct transaction ID.
        /// </summary>
        [TestMethod]
        public async Task Validation_QueriesDatabaseWithCorrectTransactionId()
        {
            var transactionId = 42;
            var command = new DeleteTransactionCommand(transactionId);

            var transactions = new List<MindedExample.Domain.Transaction>
            {
                new MindedExample.Domain.Transaction { Id = transactionId },
                new MindedExample.Domain.Transaction { Id = 99 }
            };
            Mock<DbSet<MindedExample.Domain.Transaction>> mockDbSet = transactions.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Transactions).Returns(mockDbSet.Object);

            IValidationResult result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }
    }
}

