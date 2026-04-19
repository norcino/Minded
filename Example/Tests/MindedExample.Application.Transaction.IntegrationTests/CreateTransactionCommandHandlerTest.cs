//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using MindedExample.Tests.Integration.Common;
//using MindedExample.Tests.Common.FluentAssertion;
//using MindedExample.Infrastructure.Data.Testing.Builder;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using MindedExample.Application.Transaction.Command;

//namespace MindedExample.Application.Transaction.IntegrationTests
//{
//    [TestClass]
//    public class CreateTransactionCommandHandlerTest : BaseServiceIntegrationTest
//    {
//        [TestMethod]
//        public async Task Handler_creates_new_transaction_with_the_correct_properties()
//        {
//            var category = Persister<MindedExample.Domain.Category>.New().Persist();

//            var transaction = new MindedExample.Domain.Transaction
//            {
//                CategoryId = category.Id,
//                Debit = 100,
//                Description = "Test transaction",
//                Recorded = DateTime.Now
//            };

//            var command = new CreateTransactionCommand(transaction);
//            var response = await mediator.ProcessCommandAsync<int>(command);

//            Assert.IsTrue(response.Successful, "The command response is successful");

//            var createdTransaction = await Context.Transactions.SingleAsync(p => p.Id == response.Result);

//            createdTransaction.Should().BeEq transaction, "Id");
//            Assert.IsTrue(Context.Categories.Any());
//        }
//    }
//}
