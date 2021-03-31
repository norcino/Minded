//using System;
//using Common.IntegrationTests;
//using FluentAssertion.MSTest;
//using Data.Common.Testing.Builder;
//using Data.Entity;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Linq;
//using Builder;

//namespace Application.Api.IntegrationTests
//{
//    [TestClass]
//    public class TransactionControllerTests : BaseApiIntegrationTests
//    {
//        private Persister<Transaction> _transactionPersister;
//        private Builder<Transaction> _transactionBuilder;

//        [TestInitialize]
//        public void TestInitialize()
//        {
//            _transactionPersister = new Persister<Transaction>(_context);
//            _transactionBuilder = new Builder<Transaction>();
//        }

//        [TestCleanup]
//        public void TestCleanup()
//        {
//            _transactionPersister?.Dispose();
//        }

//        #region GET
//        [TestMethod]
//        public async Task GET_support_orderBy_Description_descending()
//        {
//            const int expectedTransactions = 5;

//            _transactionPersister.Persist(expectedTransactions, (t, i) =>
//            {
//                t.Description = (expectedTransactions - i).ToString();
//            });

//            var response = await _client.GetAsync("/api/transaction?$orderby=Description desc");
//            Assert.That.IsOkHttpResponse(response);

//            var transactions = response.To<List<Transaction>>();
//            for (var i = 0; i < expectedTransactions - 1; i++)
//            {
//                Assert.IsTrue(transactions[i].Id < transactions[i + 1].Id);
//                Assert.IsTrue(int.Parse(transactions[i].Description) > int.Parse(transactions[i + 1].Description));
//            }
//        }

//        [TestMethod]
//        public async Task GET_support_orderBy_Description_ascending()
//        {
//            const int expectedTransactions = 5;

//            _transactionPersister.Persist(expectedTransactions, (t, i) =>
//            {
//                t.Description = i.ToString();
//            });

//            var response = await _client.GetAsync("/api/transaction?$orderby=Description");
//            Assert.That.IsOkHttpResponse(response);

//            var transactions = response.To<List<Transaction>>();
//            for (var i = 0; i < expectedTransactions - 1; i++)
//            {
//                Assert.IsTrue(transactions[i].Id < transactions[i + 1].Id);
//                Assert.IsTrue(int.Parse(transactions[i].Description) < int.Parse(transactions[i + 1].Description));
//            }
//        }

//        [TestMethod]
//        public async Task GET_return_all_transactions()
//        {
//            _transactionPersister.Persist(10, (t, i) =>
//            {
//                t.Description = i.ToString();
//            });

//            var categories = await _client.GetAsync("/api/transaction");

//            Assert.That.All(categories.To<List<Transaction>>()).HaveCount(10);
//        }

//        [TestMethod]
//        public async Task GET_return_all_transactions_limiting_to_the_first_100()
//        {
//            const int NumberOfTransactionsToCreate = 110;

//            var category = Persister<Category>.New().Persist();
//            _transactionPersister.Persist(NumberOfTransactionsToCreate, (t, i) =>
//            {
//                t.Category = category;
//                t.CategoryId = category.Id;
//            });

//            var transactions = await _client.GetAsync("/api/transaction");

//            Assert.That.All(transactions.To<List<Transaction>>()).HaveCount(MaxPageItemNumber);
//        }

//        [TestMethod]
//        public async Task GET_support_orderBy_Id_ascending()
//        {
//            const int NumberOfTransactionsToCreate = 3;

//            var category = Persister<Category>.New().Persist();
//            _transactionPersister.Persist(NumberOfTransactionsToCreate, (t, i) =>
//            {
//                t.Category = category;
//                t.CategoryId = category.Id;
//            });

//            var response = await _client.GetAsync("/api/transaction?&orderby=Id");
//            var transactions = response.To<List<Transaction>>();

//            Assert.That.All(transactions).HaveCount(NumberOfTransactionsToCreate);
//            Assert.IsTrue(transactions[0].Id < transactions[1].Id &&
//                          transactions[1].Id < transactions[2].Id);
//        }

//        [TestMethod]
//        public async Task GET_support_orderBy_Id_descending()
//        {
//            const int NumberOfTransactionsToCreate = 3;

//            var category = Persister<Category>.New().Persist();
//            _transactionPersister.Persist(NumberOfTransactionsToCreate, (t, i) =>
//            {
//                t.Category = category;
//                t.CategoryId = category.Id;
//            });

//            var response = await _client.GetAsync("/api/transaction?$orderby=Id desc");
//            var transactions = response.To<List<Transaction>>();

//            Assert.That.All(transactions).HaveCount(NumberOfTransactionsToCreate);
//            Assert.IsTrue(transactions[0].Id > transactions[1].Id &&
//                          transactions[1].Id > transactions[2].Id);
//        }
//        #endregion

//        #region GET by ID
//        [TestMethod]
//        public async Task GET_byId_returns_404_when_id_does_not_exist()
//        {
//            var response = await _client.GetAsync("/api/transaction/1");
//            Assert.That.IsNotFoundHttpResponse(response);
//        }

//        [TestMethod]
//        public async Task GET_byId_returns_ok_200_when_entity_with_specified_Id_exists()
//        {
//            var transaction = _transactionPersister.Persist();
//            var response = await _client.GetAsync($"/api/transaction/{transaction.Id}");
//            Assert.That.IsOkHttpResponse(response);
//        }

//        [TestMethod]
//        public async Task GET_byId_returns_correct_and_complete_entity_with_the_specified_Id()
//        {
//            var expectedTransaction = _transactionPersister.Persist();

//            var response = await _client.GetAsync($"/api/transaction/{expectedTransaction.Id}");
//            var transaction = response.To<Transaction>();
//            Assert.IsNotNull(transaction);
//            Assert.That.This(expectedTransaction).HasSameProperties(transaction);
//        }
//        #endregion

//        #region POST
//        [TestMethod]
//        public async Task POST_returns_201_passing_valid_entity()
//        {
//            var category = Persister<Category>.New().Persist();
//            var expectedTransaction = _transactionBuilder.Build(t =>
//            {
//                t.Id = 0;
//                t.CategoryId = category.Id;
//            });
//            var response = await _client.PostAsync<Transaction>("/api/transaction", expectedTransaction);

//            Assert.That.IsCreatedHttpResponse(response);
//        }

//        [TestMethod]
//        public async Task POST_location_URI_to_access_the_created_entity()
//        {
//            var category = Persister<Category>.New().Persist();
//            var expectedTransaction = _transactionBuilder.Build(t =>
//            {
//                t.Id = 0;
//                t.CategoryId = category.Id;
//            });
//            var postResponse = await _client.PostAsync<Transaction>("/api/transaction", expectedTransaction);

//            Assert.IsNotNull(postResponse.Headers.Location.AbsoluteUri, "Location should be set with the URL to the created object");
//            Assert.IsTrue(postResponse.Headers.Location.AbsoluteUri.Contains("/api/Transaction/"), "Entity URI has targets the right position");
//        }

//        [TestMethod]
//        public async Task POST_creates_valid_entity()
//        {
//            var category = Persister<Category>.New().Persist();
//            var expectedTransaction = _transactionBuilder.Build(t =>
//            {
//                t.Id = 0;
//                t.CategoryId = category.Id;
//            });
//            var postResponse = await _client.PostAsync<Transaction>("/api/transaction", expectedTransaction);
//            var getResponse = await _client.GetAsync(postResponse.Headers.Location.AbsoluteUri);

//            Assert.That.IsOkHttpResponse(getResponse);
//            var transaction = getResponse.To<Transaction>();
//            Assert.IsNotNull(transaction);
//            Assert.That.This(expectedTransaction).HasSameProperties(transaction, nameof(transaction.Id));
//        }

//        [TestMethod]
//        public async Task POST_return_BadRequest_400_trying_to_create_entity_with_Id()
//        {
//            var category = Persister<Category>.New().Persist();
//            var expectedTransaction = _transactionBuilder.Build(t =>
//            {
//                t.Id = 1;
//                t.CategoryId = category.Id;
//            });
//            var postResponse = await _client.PostAsync<Transaction>("/api/transaction", expectedTransaction);
//            Assert.That.IsBadRequestHttpResponse(postResponse);
//        }
//        #endregion
//    }
//}
