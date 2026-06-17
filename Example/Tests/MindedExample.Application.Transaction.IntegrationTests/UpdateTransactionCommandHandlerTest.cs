//using System;
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
//    public class UpdateTransactionCommandHandlerTest : BaseServiceIntegrationTest
//    {
//        [TestMethod]
//        public async Task Handler_update_transaction_with_the_correct_properties()
//        {
//            var category = Persister<MindedExample.Domain.Category>.New().Persist();            
//            var categoryTwo = Persister<MindedExample.Domain.Category>.New().Persist();
//            var transaction = Persister<MindedExample.Domain.Transaction>.New().Persist(t =>
//            {
//                t.CategoryId = category.Id;
//            });

//            transaction.CategoryId = categoryTwo.Id;
//            transaction.Debit += 100;
//            transaction.Credit -= 100;
//            transaction.Description = transaction.Description + "2";
//            transaction.Recorded = transaction.Recorded.AddDays(10);

//            var command = new UpdateTransactionCommand(transaction.Id, transaction);
//            var response = await mediator.ProcessCommandAsync<MindedExample.Domain.Transaction>(command);

//            Assert.IsTrue(response.Successful, "The command response is successful");

//            var updateTransaction = await Context.Transactions.AsNoTracking().SingleAsync(p => p.Id == response.Result.Id);

//            Assert.That.This(updateTransaction).HasSameProperties(transaction);
//        }
//    }
//}
