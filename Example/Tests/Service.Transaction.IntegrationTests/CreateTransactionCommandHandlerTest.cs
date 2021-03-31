//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Common.IntegrationTests;
//using Common.Tests.FluentAssertion;
//using Data.Common.Testing.Builder;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Service.Transaction.Command;

//namespace Service.Transaction.IntegrationTests
//{
//    [TestClass]
//    public class CreateTransactionCommandHandlerTest : BaseServiceIntegrationTest
//    {
//        [TestMethod]
//        public async Task Handler_creates_new_transaction_with_the_correct_properties()
//        {
//            var category = Persister<Data.Entity.Category>.New().Persist();

//            var transaction = new Data.Entity.Transaction
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
