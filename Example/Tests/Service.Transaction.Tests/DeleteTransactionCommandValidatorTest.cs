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
using Service.Transaction.Command;
using Service.Transaction.Validator;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using QM.Common.Testing;

namespace Service.Transaction.Tests
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

            var transactions = new List<Data.Entity.Transaction>
            {
                new Data.Entity.Transaction { Id = transactionId }
            };
            var mockDbSet = transactions.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Transactions).Returns(mockDbSet.Object);

            var result = await _sut.ValidateAsync(command);

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

            var transactions = new List<Data.Entity.Transaction>();
            var mockDbSet = transactions.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Transactions).Returns(mockDbSet.Object);

            var result = await _sut.ValidateAsync(command);

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

            var transactions = new List<Data.Entity.Transaction>
            {
                new Data.Entity.Transaction { Id = transactionId },
                new Data.Entity.Transaction { Id = 99 }
            };
            var mockDbSet = transactions.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Transactions).Returns(mockDbSet.Object);

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }
    }
}

