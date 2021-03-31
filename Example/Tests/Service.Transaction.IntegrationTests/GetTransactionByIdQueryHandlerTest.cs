//using System;
//using System.Globalization;
//using System.Threading.Tasks;
//using Common.IntegrationTests;
//using Common.Tests.FluentAssertion;
//using Data.Common.Testing.Builder;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Service.Transaction.Query;

//namespace Service.Transaction.IntegrationTests
//{
//    [TestClass]
//    public class GetTransactionByIdQueryHandlerTest : BaseServiceIntegrationTest
//    {
//        [TestMethod]
//        public async Task Handler_get_transaction_by_id_with_the_correct_properties()
//        {
//            var transaction = Persister<Data.Entity.Transaction>.New().Persist();
                        
//            var query = new GetTransactionByIdQuery(transaction.Id);
//            var dbTransaction = await mediator.ProcessQueryAsync(query);

//            Assert.IsNotNull(dbTransaction);
//            Assert.That.This(dbTransaction).HasSameProperties(transaction);
//        }
//    }
//}
