using Data.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Builder;
using FluentAssertions;
using System.Net;
using AnonymousData;
using Common.Tests;
using Common.E2ETests;
using Minded.Framework.CQRS.Abstractions;
using QM.Common.Testing;

namespace Application.Api.IntegrationTests
{
    /// <summary>
    /// Integration tests for Transaction API endpoints.
    /// Tests CRUD operations, OData query options, and RestMediator response rules.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TransactionE2ETests : BaseE2ETest
    {
        #region GET - OData Expand Navigation Properties
        [TestMethod]
        public async Task GET_without_expand_should_not_include_Category_navigation_property()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            await Seed<Transaction>(t => t.Id, 5, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.GetAsync("/api/transaction");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();

            // Navigation properties should NOT be serialized without $expand
            content.Should().NotContain("\"Category\"");
        }

        [TestMethod]
        public async Task GET_with_expand_Category_should_include_Category_navigation_property()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            await Seed<Transaction>(t => t.Id, 5, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.GetAsync("/api/transaction?$expand=Category");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();

            // Category navigation property should be serialized with $expand
            content.Should().Contain("\"Category\"");
            content.Should().Contain($"\"Name\":\"{category.Name}\"");
        }

        [TestMethod]
        public async Task GET_byId_without_expand_should_not_include_navigation_properties()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            Transaction transaction = (await Seed<Transaction>(t => t.Id, 1, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            })).First();

            var response = await _sutClient.GetAsync($"/api/transaction/{transaction.Id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();

            // Navigation properties should NOT be serialized without $expand
            content.Should().NotContain("\"Category\"");
            content.Should().NotContain("\"User\"");
        }

        [TestMethod]
        public async Task GET_with_expand_multiple_navigation_properties_should_include_all_expanded_properties()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            await Seed<Transaction>(t => t.Id, 3, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.GetAsync("/api/transaction?$expand=Category");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();

            // Both navigation properties should be serialized
            content.Should().Contain("\"Category\"");
        }
        #endregion

        #region GET - OData OrderBy
        [TestMethod]
        public async Task GET_should_support_OrderBy_Description_ascending()
        {
            const int expectedTransactions = 5;
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });

            await Seed<Transaction>(t => t.Id, expectedTransactions, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
                t.Description = $"Transaction {i}";
            });

            var response = await _sutClient.GetAsync("/api/transaction?$orderby=Description");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            transactions.Should().HaveCount(expectedTransactions);
            transactions.Should().BeInAscendingOrder(t => t.Description);
        }

        [TestMethod]
        public async Task GET_should_support_OrderBy_Description_descending()
        {
            const int expectedTransactions = 5;
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });

            await Seed<Transaction>(t => t.Id, expectedTransactions, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
                t.Description = $"Transaction {i}";
            });

            var response = await _sutClient.GetAsync("/api/transaction?$orderby=Description desc");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            transactions.Should().HaveCount(expectedTransactions);
            transactions.Should().BeInDescendingOrder(t => t.Description);
        }

        [TestMethod]
        public async Task GET_should_support_OrderBy_Id_ascending()
        {
            const int numberOfTransactions = 10;
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });

            await Seed<Transaction>(t => t.Id, numberOfTransactions, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.GetAsync("/api/transaction?$orderby=Id");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            transactions.Should().HaveCount(numberOfTransactions);
            transactions.Should().BeInAscendingOrder(t => t.Id);
        }

        [TestMethod]
        public async Task GET_should_support_OrderBy_Id_descending()
        {
            const int numberOfTransactions = 10;
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });

            await Seed<Transaction>(t => t.Id, numberOfTransactions, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.GetAsync("/api/transaction?$orderby=Id desc");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            transactions.Should().HaveCount(numberOfTransactions);
            transactions.Should().BeInDescendingOrder(t => t.Id);
        }
        #endregion

        #region GET - OData Filter
        [TestMethod]
        public async Task GET_should_support_filter_by_Description()
        {
            const int numberOfTransactions = 10;
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            var targetDescription = "Special Transaction";

            await Seed<Transaction>(t => t.Id, numberOfTransactions, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
                t.Description = i == 5 ? targetDescription : $"Transaction {i}";
            });

            var response = await _sutClient.GetAsync($"/api/transaction?$filter=Description eq '{targetDescription}'");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            transactions.Should().HaveCount(1);
            transactions.First().Description.Should().Be(targetDescription);
        }

        [TestMethod]
        public async Task GET_should_support_filter_by_CategoryId()
        {
            User user = await SeedOne<User>(u => u.Id);
            var categories = (await Seed<Category>(c => c.Id, 2, (c, i) => { c.UserId = user.Id; c.ParentId = null; })).ToList();

            await Seed<Transaction>(t => t.Id, 5, (t, i) =>
            {
                t.CategoryId = i % 2 == 0 ? categories[0].Id : categories[1].Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.GetAsync($"/api/transaction?$filter=CategoryId eq {categories[0].Id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            transactions.Should().HaveCountGreaterThan(0);
            transactions.Should().OnlyContain(t => t.CategoryId == categories[0].Id);
        }

        [TestMethod]
        public async Task GET_should_support_filter_by_Credit_greater_than()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });

            await Seed<Transaction>(t => t.Id, 10, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
                t.Credit = i * 10;
                t.Debit = 0;
            });

            var response = await _sutClient.GetAsync("/api/transaction?$filter=Credit gt 50");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            transactions.Should().OnlyContain(t => t.Credit > 50);
        }
        #endregion

        #region GET - OData Top and Skip
        [TestMethod]
        public async Task GET_should_support_Top_parameter()
        {
            const int numberOfTransactions = 50;
            const int topValue = 10;
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });

            await Seed<Transaction>(t => t.Id, numberOfTransactions, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.GetAsync($"/api/transaction?$top={topValue}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            transactions.Should().HaveCount(topValue);
        }

        [TestMethod]
        public async Task GET_should_support_Skip_parameter()
        {
            const int numberOfTransactions = 20;
            const int skipValue = 10;
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });

            await Seed<Transaction>(t => t.Id, numberOfTransactions, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.GetAsync($"/api/transaction?$skip={skipValue}&$orderby=Id");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            transactions.Should().HaveCount(numberOfTransactions - skipValue);
        }

        [TestMethod]
        public async Task GET_should_support_Top_and_Skip_together_for_pagination()
        {
            const int numberOfTransactions = 50;
            const int pageSize = 10;
            const int pageNumber = 2; // Second page
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });

            await Seed<Transaction>(t => t.Id, numberOfTransactions, (t, i) =>
            {
                t.Id = i;
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.GetAsync($"/api/transaction?$top={pageSize}&$skip={pageSize * (pageNumber - 1)}&$orderby=Id");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            transactions.Should().HaveCount(pageSize);
        }

        [TestMethod]
        public async Task GET_should_return_SetLimit_when_Top_exceeds_maximum()
        {
            const int topValue = MaxPageItemNumber + 1;
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.Id = i; c.UserId = user.Id; c.ParentId = null; });

            await Seed<Transaction>(t => t.Id, 200, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.GetAsync($"/api/transaction?$top={topValue}");
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            transactions.Should().HaveCount(100);
        }
        #endregion

        //#region GET - OData Count
        //[TestMethod]
        //public async Task GET_should_support_count_parameter()
        //{
        //    const int numberOfTransactions = 25;
        //    User user = await SeedOne<User>(u => u.Id);
        //    Category category = await SeedOne<Category>(c => c.Id, (c, i) => c.UserId = user.Id);

        //    await Seed<Transaction>(t => t.Id, numberOfTransactions, (t, i) =>
        //    {
        //        t.CategoryId = category.Id;
        //        t.UserId = user.Id;
        //    });

        //    var response = await _sutClient.GetAsync("/api/transaction?$count=true");

        //    response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        //    var content = await response.Content.ReadAsStringAsync();

        //    // OData count response includes @odata.count property
        //    content.Should().Contain("@odata.count");
        //    content.Should().Contain($"\"@odata.count\":{numberOfTransactions}");
        //}
        //#endregion

        //#region GET - OData Select
        //[TestMethod]
        //public async Task GET_should_support_select_specific_properties()
        //{
        //    User user = await SeedOne<User>(u => u.Id);
        //    Category category = await SeedOne<Category>(c => c.Id, (c, i) => c.UserId = user.Id);

        //    await Seed<Transaction>(t => t.Id, 5, (t, i) =>
        //    {
        //        t.CategoryId = category.Id;
        //        t.UserId = user.Id;
        //    });

        //    var response = await _sutClient.GetAsync("/api/transaction?$select=Id,Description");

        //    response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        //    var content = await response.Content.ReadAsStringAsync();

        //    // Should contain Id and Description
        //    content.Should().Contain("\"Id\"");
        //    content.Should().Contain("\"Description\"");

        //    // Should NOT contain Credit, Debit, or CategoryId
        //    content.Should().NotContain("\"Credit\"");
        //    content.Should().NotContain("\"Debit\"");
        //}
        //#endregion

        #region GET - All Transactions
        [TestMethod]
        public async Task GET_should_return_empty_list_and_200Ok_when_no_transactions_exist()
        {
            var response = await _sutClient.GetAsync("/api/transaction");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            transactions.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GET_should_return_all_transactions_and_200Ok()
        {
            const int numberOfTransactions = 10;
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });

            await Seed<Transaction>(t => t.Id, numberOfTransactions, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.GetAsync("/api/transaction");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            transactions.Should().HaveCount(numberOfTransactions);
        }

        [TestMethod]
        public async Task GET_should_limit_results_to_maximum_page_size()
        {
            const int numberOfTransactions = MaxPageItemNumber + 10;
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });

            await Seed<Transaction>(t => t.Id, numberOfTransactions, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.GetAsync("/api/transaction");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<Transaction> transactions = await response.Content.ReadAsAsync<List<Transaction>>();

            transactions.Should().HaveCount(MaxPageItemNumber);
        }
        #endregion

        #region GET by ID
        [TestMethod]
        public async Task GET_byId_should_return_404NotFound_when_Id_does_not_exist()
        {
            var response = await _sutClient.GetAsync("/api/transaction/999999");

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GET_byId_should_return_200Ok_when_transaction_exists()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            Transaction transaction = (await Seed<Transaction>(t => t.Id, 1, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            })).First();

            var response = await _sutClient.GetAsync($"/api/transaction/{transaction.Id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task GET_byId_should_return_correct_transaction()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            Transaction expectedTransaction = (await Seed<Transaction>(t => t.Id, 1, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            })).First();

            var response = await _sutClient.GetAsync($"/api/transaction/{expectedTransaction.Id}");
            Transaction transaction = await response.Content.ReadAsAsync<Transaction>();

            transaction.Should().BeEquivalentTo(expectedTransaction, options => options.Excluding(t => t.Id));
        }
        #endregion

        #region POST (Create)
        [TestMethod]
        public async Task POST_should_return_201Created_with_valid_transaction()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            Transaction newTransaction = Builder<Transaction>.New().Build(t =>
            {
                t.Id = 0;
                t.CategoryId = category.Id;
                t.UserId = user.Id;
                t.Credit = Any.Decimal();
                t.Debit = 0;
            });

            var response = await _sutClient.PostAsync("/api/transaction", newTransaction);

            response.Should().HaveHttpStatusCode(HttpStatusCode.Created);
        }

        [TestMethod]
        public async Task POST_should_create_transaction_with_correct_values()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            Transaction newTransaction = Builder<Transaction>.New().Build(t =>
            {
                t.Id = 0;
                t.CategoryId = category.Id;
                t.UserId = user.Id;
                t.Credit = Any.Decimal();
                t.Debit = 0;
            });

            var response = await _sutClient.PostAsync("/api/transaction", newTransaction);
            Transaction createdTransaction = await response.Content.ReadAsAsync<Transaction>();

            createdTransaction.Should().BeEquivalentTo(newTransaction, options => options.Excluding(t => t.Id));
            createdTransaction.Id.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public async Task POST_should_return_400BadRequest_when_Id_is_specified()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            Transaction newTransaction = Builder<Transaction>.New().Build(t =>
            {
                t.Id = Any.Int();
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.PostAsync("/api/transaction", newTransaction);

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
            response.Should().ContainOutcomeEntry("{0} should not be specified on creation", "Id", Severity.Error);
        }

        [TestMethod]
        public async Task POST_should_return_400BadRequest_when_CategoryId_does_not_exist()
        {
            User user = await SeedOne<User>(u => u.Id);
            Transaction newTransaction = Builder<Transaction>.New().Build(t =>
            {
                t.Id = 0;
                t.CategoryId = 999999; // Non-existent category
                t.UserId = user.Id;
            });

            var response = await _sutClient.PostAsync("/api/transaction", newTransaction);

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
        }
        #endregion

        #region PUT (Update)
        [TestMethod]
        public async Task PUT_should_return_200Ok_when_updating_existing_transaction()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            Transaction seededTransaction = (await Seed<Transaction>(t => t.Id, 1, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            })).First();

            // Create a new transaction object without navigation properties to avoid circular reference
            Transaction transaction = Builder<Transaction>.New().Build(t =>
            {
                t.Id = seededTransaction.Id;
                t.CategoryId = seededTransaction.CategoryId;
                t.UserId = seededTransaction.UserId;
                t.Recorded = seededTransaction.Recorded;
                t.Debit = seededTransaction.Debit;
                t.Description = Any.String();
                t.Credit = Any.Decimal();
            });

            var response = await _sutClient.PutAsync($"/api/transaction/{transaction.Id}", transaction);

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task PUT_should_update_transaction_with_new_values()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            Transaction seededTransaction = (await Seed<Transaction>(t => t.Id, 1, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            })).First();

            var newDescription = Any.String();
            var newCredit = Any.Decimal();

            // Create a new transaction object without navigation properties to avoid circular reference
            Transaction transaction = Builder<Transaction>.New().Build(t =>
            {
                t.Id = seededTransaction.Id;
                t.CategoryId = seededTransaction.CategoryId;
                t.UserId = seededTransaction.UserId;
                t.Recorded = seededTransaction.Recorded;
                t.Debit = seededTransaction.Debit;
                t.Description = newDescription;
                t.Credit = newCredit;
            });

            await _sutClient.PutAsync($"/api/transaction/{transaction.Id}", transaction);

            var response = await _sutClient.GetAsync($"/api/transaction/{transaction.Id}");
            Transaction updatedTransaction = await response.Content.ReadAsAsync<Transaction>();

            updatedTransaction.Description.Should().Be(newDescription);
            updatedTransaction.Credit.Should().Be(newCredit);
        }

        [TestMethod]
        public async Task PUT_should_return_404NotFound_when_transaction_does_not_exist()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            Transaction transaction = Builder<Transaction>.New().Build(t =>
            {
                t.Id = 999999;
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.PutAsync($"/api/transaction/{transaction.Id}", transaction);

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task PUT_should_return_400BadRequest_when_Id_in_body_does_not_match_Id_in_url()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });

            // Create a new transaction object without navigation properties to avoid circular reference
            Transaction transaction = Builder<Transaction>.New().Build(t =>
            {
                t.Id = Any.Int();
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var differentId = transaction.Id + 1;

            var response = await _sutClient.PutAsync($"/api/transaction/{differentId}", transaction);

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
        }
        #endregion

        #region DELETE
        [TestMethod]
        public async Task DELETE_should_return_200Ok_when_transaction_deleted()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            Transaction transaction = (await Seed<Transaction>(t => t.Id, 1, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            })).First();

            var response = await _sutClient.DeleteAsync($"/api/transaction/{transaction.Id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            // Verify transaction is deleted
            response = await _sutClient.GetAsync($"/api/transaction/{transaction.Id}");
            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DELETE_should_return_404_when_transaction_not_found()
        {
            var response = await _sutClient.DeleteAsync("/api/transaction/999999");

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }
        #endregion
    }
}
