using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MindedExample.Api.Models;
using MindedExample.Domain;
using MindedExample.Tests.Common;
using MindedExample.Tests.E2E.Common;
using QM.Common.Testing;

namespace MindedExample.Api.E2ETests
{
    /// <summary>
    /// Cross-tenant isolation tests: tenant B must never be able to read or mutate tenant A's
    /// data through any tenant-scoped endpoint. Two tenants are registered through the public
    /// API; tenant A owns a category and a transaction. Cross-tenant reads return 404 rather
    /// than 403 so the existence of other tenants' data is not leaked.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TenantIsolationE2ETests : BaseE2ETest
    {
        protected override bool UseTestAuthentication => false;

        private AuthResponse _ownerA;
        private AuthResponse _ownerB;
        private Category _categoryA;
        private Transaction _transactionA;

        [TestInitialize]
        public async Task ArrangeTenants()
        {
            _ownerA = await RegisterTenantOwnerAsync();
            _ownerB = await RegisterTenantOwnerAsync();

            UseBearer(_ownerA.AccessToken);
            var categoryResponse = await _sutClient.PostAsync("/api/category",
                new { Name = $"Cat{Any.String()}", Description = Any.String(), Active = true, UserId = _ownerA.User.Id });
            categoryResponse.Should().HaveHttpStatusCode(HttpStatusCode.Created);
            _categoryA = await categoryResponse.Content.ReadAsAsync<Category>();

            var transactionResponse = await _sutClient.PostAsync("/api/transaction", new
            {
                Description = $"Tx{Any.String()}",
                Credit = 10.5m,
                Debit = 0m,
                Recorded = DateTime.UtcNow,
                CategoryId = _categoryA.Id,
                UserId = _ownerA.User.Id
            });
            transactionResponse.Should().HaveHttpStatusCode(HttpStatusCode.Created);
            _transactionA = await transactionResponse.Content.ReadAsAsync<Transaction>();
        }

        #region List endpoints are tenant-scoped
        [TestMethod]
        public async Task GET_categories_should_not_return_other_tenants_rows()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.GetAsync("/api/category");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var categories = await response.Content.ReadAsAsync<List<Category>>();
            categories.Should().NotContain(c => c.Id == _categoryA.Id);
        }

        [TestMethod]
        public async Task GET_transactions_should_not_return_other_tenants_rows()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.GetAsync("/api/transaction");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var transactions = await response.Content.ReadAsAsync<List<Transaction>>();
            transactions.Should().NotContain(t => t.Id == _transactionA.Id);
        }

        [TestMethod]
        public async Task GET_users_should_return_only_own_tenant_users()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.GetAsync("/api/users");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var users = await response.Content.ReadAsAsync<List<User>>();
            users.Should().OnlyContain(u => u.TenantId == _ownerB.User.TenantId);
            users.Should().NotContain(u => u.Id == _ownerA.User.Id);
        }

        [TestMethod]
        public async Task GET_users_with_roles_should_return_only_own_tenant_users()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.GetAsync("/api/users-with-roles");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var users = await response.Content.ReadAsAsync<List<User>>();
            users.Should().NotContain(u => u.Id == _ownerA.User.Id);
        }
        #endregion

        #region Direct id access does not leak existence
        [TestMethod]
        public async Task GET_category_by_id_of_another_tenant_should_return_404NotFound()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.GetAsync($"/api/category/{_categoryA.Id}");

            // 404 (not 403): the response must not reveal that the entity exists
            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GET_transaction_by_id_of_another_tenant_should_return_404NotFound()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.GetAsync($"/api/transaction/{_transactionA.Id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GET_user_by_id_of_another_tenant_should_return_404NotFound()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.GetAsync($"/api/users/{_ownerA.User.Id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }
        #endregion

        #region Cross-tenant mutations are rejected and have no effect
        [TestMethod]
        public async Task PUT_category_of_another_tenant_should_fail_and_leave_data_unchanged()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.PutAsync($"/api/category/{_categoryA.Id}", new
            {
                Id = _categoryA.Id,
                Name = "Hijacked",
                Description = "Hijacked",
                Active = false,
                UserId = _ownerB.User.Id
            });
            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);

            UseBearer(_ownerA.AccessToken);
            var reread = await _sutClient.GetAsync($"/api/category/{_categoryA.Id}");
            reread.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            (await reread.Content.ReadAsAsync<Category>()).Name.Should().Be(_categoryA.Name);
        }

        [TestMethod]
        public async Task DELETE_category_of_another_tenant_should_fail_and_leave_data_in_place()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.DeleteAsync($"/api/category/{_categoryA.Id}");
            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);

            UseBearer(_ownerA.AccessToken);
            var reread = await _sutClient.GetAsync($"/api/category/{_categoryA.Id}");
            reread.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task PUT_transaction_of_another_tenant_should_fail_and_leave_data_unchanged()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.PutAsync($"/api/transaction/{_transactionA.Id}", new
            {
                Id = _transactionA.Id,
                Description = "Hijacked",
                Credit = 999m,
                Debit = 0m,
                Recorded = DateTime.UtcNow,
                CategoryId = _categoryA.Id,
                UserId = _ownerB.User.Id
            });
            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);

            UseBearer(_ownerA.AccessToken);
            var reread = await _sutClient.GetAsync($"/api/transaction/{_transactionA.Id}");
            reread.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            (await reread.Content.ReadAsAsync<Transaction>()).Description.Should().Be(_transactionA.Description);
        }

        [TestMethod]
        public async Task DELETE_transaction_of_another_tenant_should_fail_and_leave_data_in_place()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.DeleteAsync($"/api/transaction/{_transactionA.Id}");
            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);

            UseBearer(_ownerA.AccessToken);
            var reread = await _sutClient.GetAsync($"/api/transaction/{_transactionA.Id}");
            reread.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task DELETE_user_of_another_tenant_should_fail_and_user_remains()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.DeleteAsync($"/api/users/{_ownerA.User.Id}");
            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);

            // Tenant A's owner can still log in: nothing was deleted
            UseAnonymous();
            var login = await _sutClient.PostAsync("/api/auth/login",
                new LoginRequest { Email = _ownerA.User.Email, Password = DefaultTestPassword });
            login.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        }
        #endregion

        #region OData options cannot escape the tenant scope
        [TestMethod]
        public async Task GET_categories_with_filter_matching_foreign_data_should_return_empty()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.GetAsync($"/api/category?$filter=Name eq '{_categoryA.Name}'");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var categories = await response.Content.ReadAsAsync<List<Category>>();
            categories.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GET_transactions_with_expand_should_not_reveal_foreign_rows()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.GetAsync("/api/transaction?$expand=Category");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var transactions = await response.Content.ReadAsAsync<List<Transaction>>();
            transactions.Should().NotContain(t => t.Id == _transactionA.Id);
            transactions.Should().NotContain(t => t.Category != null && t.Category.Id == _categoryA.Id);
        }

        [TestMethod]
        public async Task GET_users_with_filter_on_foreign_email_should_return_empty()
        {
            UseBearer(_ownerB.AccessToken);
            var response = await _sutClient.GetAsync($"/api/users?$filter=Email eq '{_ownerA.User.Email}'");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var users = await response.Content.ReadAsAsync<List<User>>();
            users.Should().BeEmpty();
        }
        #endregion
    }
}
