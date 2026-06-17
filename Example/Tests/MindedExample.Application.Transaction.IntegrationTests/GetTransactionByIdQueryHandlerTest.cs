//using System;
//using System.Globalization;
//using System.Threading.Tasks;
//using MindedExample.Tests.Integration.Common;
//using MindedExample.Tests.Common.FluentAssertion;
//using MindedExample.Infrastructure.Data.Testing.Builder;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using MindedExample.Application.Transaction.Query;

//namespace MindedExample.Application.Transaction.IntegrationTests
//{
//    [TestClass]
//    public class GetTransactionByIdQueryHandlerTest : BaseServiceIntegrationTest
//    {
//        [TestMethod]
//        public async Task Handler_get_transaction_by_id_with_the_correct_properties()
//        {
//            var transaction = Persister<MindedExample.Domain.Transaction>.New().Persist();
                        
//            var query = new GetTransactionByIdQuery(transaction.Id);
//            var dbTransaction = await mediator.ProcessQueryAsync(query);

//            Assert.IsNotNull(dbTransaction);
//            Assert.That.This(dbTransaction).HasSameProperties(transaction);
//        }
//    }
//}
