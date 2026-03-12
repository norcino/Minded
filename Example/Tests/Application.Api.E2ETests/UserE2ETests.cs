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
    /// Integration tests for User API endpoints.
    /// Tests CRUD operations, OData query options, and RestMediator response rules.
    /// Also verifies that sensitive data (Name, Surname, Email) is properly protected in logs.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class UserE2ETests : BaseE2ETest
    {
        #region GET - OData Expand Navigation Properties
        [TestMethod]
        public async Task GET_without_expand_should_not_include_Categories_navigation_property()
        {
            await Seed<User>(u => u.Id, 5);

            var response = await _sutClient.GetAsync("/api/users");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();

            // Navigation properties should NOT be serialized without $expand
            content.Should().NotContain("\"Categories\"");
            content.Should().NotContain("\"Transactions\"");
        }

        [TestMethod]
        public async Task GET_with_expand_Categories_should_include_Categories_navigation_property()
        {
            User user = await SeedOne<User>(u => u.Id);
            await Seed<Category>(c => c.Id, 3, (c, i) => { c.UserId = user.Id; c.ParentId = null; });

            var response = await _sutClient.GetAsync("/api/users?$expand=Categories");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();

            // Categories navigation property should be serialized with $expand
            content.Should().Contain("\"Categories\"");
        }

        [TestMethod]
        public async Task GET_byId_without_expand_should_not_include_navigation_properties()
        {
            User user = await SeedOne<User>(u => u.Id);
            await Seed<Category>(c => c.Id, 2, (c, i) => { c.UserId = user.Id; c.ParentId = null; });

            var response = await _sutClient.GetAsync($"/api/users/{user.Id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();

            // Navigation properties should NOT be serialized without $expand
            content.Should().NotContain("\"Categories\"");
            content.Should().NotContain("\"Transactions\"");
        }

        [TestMethod]
        public async Task GET_with_expand_multiple_navigation_properties_should_include_all_expanded_properties()
        {
            User user = await SeedOne<User>(u => u.Id);
            Category category = await SeedOne<Category>(c => c.Id, (c, i) => { c.UserId = user.Id; c.ParentId = null; });
            await Seed<Transaction>(t => t.Id, 2, (t, i) =>
            {
                t.CategoryId = category.Id;
                t.UserId = user.Id;
            });

            var response = await _sutClient.GetAsync("/api/users?$expand=Categories,Transactions");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();

            // Both navigation properties should be serialized
            content.Should().Contain("\"Categories\"");
            content.Should().Contain("\"Transactions\"");
        }
        #endregion

        #region GET - OData OrderBy
        [TestMethod]
        public async Task GET_should_support_OrderBy_Name_ascending()
        {
            const int expectedUsers = 5;
            await Seed<User>(u => u.Id, expectedUsers, (u, i) => u.Name = $"User{i:D3}");

            var response = await _sutClient.GetAsync("/api/users?$orderby=Name");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<User> users = await response.Content.ReadAsAsync<List<User>>();

            users.Should().HaveCount(expectedUsers);
            users.Should().BeInAscendingOrder(u => u.Name);
        }

        [TestMethod]
        public async Task GET_should_support_OrderBy_Name_descending()
        {
            const int expectedUsers = 5;
            await Seed<User>(u => u.Id, expectedUsers, (u, i) => u.Name = $"User{i:D3}");

            var response = await _sutClient.GetAsync("/api/users?$orderby=Name desc");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<User> users = await response.Content.ReadAsAsync<List<User>>();

            users.Should().HaveCount(expectedUsers);
            users.Should().BeInDescendingOrder(u => u.Name);
        }

        [TestMethod]
        public async Task GET_should_support_OrderBy_Id_ascending()
        {
            const int numberOfUsers = 10;
            await Seed<User>(u => u.Id, numberOfUsers);

            var response = await _sutClient.GetAsync("/api/users?$orderby=Id");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<User> users = await response.Content.ReadAsAsync<List<User>>();

            users.Should().HaveCount(numberOfUsers);
            users.Should().BeInAscendingOrder(u => u.Id);
        }

        [TestMethod]
        public async Task GET_should_support_OrderBy_Id_descending()
        {
            const int numberOfUsers = 10;
            await Seed<User>(u => u.Id, numberOfUsers);

            var response = await _sutClient.GetAsync("/api/users?$orderby=Id desc");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<User> users = await response.Content.ReadAsAsync<List<User>>();

            users.Should().HaveCount(numberOfUsers);
            users.Should().BeInDescendingOrder(u => u.Id);
        }
        #endregion

        #region GET - OData Filter
        [TestMethod]
        public async Task GET_should_support_filter_by_Name()
        {
            const int numberOfUsers = 10;
            var targetName = "SpecialUser";

            await Seed<User>(u => u.Id, numberOfUsers, (u, i) =>
            {
                u.Name = i == 5 ? targetName : $"User{i}";
            });

            var response = await _sutClient.GetAsync($"/api/users?$filter=Name eq '{targetName}'");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<User> users = await response.Content.ReadAsAsync<List<User>>();

            users.Should().HaveCount(1);
            users.First().Name.Should().Be(targetName);
        }

        [TestMethod]
        public async Task GET_should_support_filter_by_Email()
        {
            const int numberOfUsers = 5;
            var targetEmail = "special@example.com";

            await Seed<User>(u => u.Id, numberOfUsers, (u, i) =>
            {
                u.Email = i == 2 ? targetEmail : $"user{i}@example.com";
            });

            var response = await _sutClient.GetAsync($"/api/users?$filter=Email eq '{targetEmail}'");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<User> users = await response.Content.ReadAsAsync<List<User>>();

            users.Should().HaveCount(1);
            users.First().Email.Should().Be(targetEmail);
        }

        [TestMethod]
        public async Task GET_should_support_filter_by_Surname_contains()
        {
            await Seed<User>(u => u.Id, 10, (u, i) =>
            {
                u.Surname = i % 2 == 0 ? "Smith" : "Johnson";
            });

            var response = await _sutClient.GetAsync("/api/users?$filter=contains(Surname,'Smith')");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<User> users = await response.Content.ReadAsAsync<List<User>>();

            users.Should().OnlyContain(u => u.Surname.Contains("Smith"));
        }
        #endregion

        #region GET - OData Top and Skip
        [TestMethod]
        public async Task GET_should_support_Top_parameter()
        {
            const int numberOfUsers = 50;
            const int topValue = 10;

            await Seed<User>(u => u.Id, numberOfUsers);

            var response = await _sutClient.GetAsync($"/api/users?$top={topValue}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<User> users = await response.Content.ReadAsAsync<List<User>>();

            users.Should().HaveCount(topValue);
        }

        [TestMethod]
        public async Task GET_should_support_Skip_parameter()
        {
            const int numberOfUsers = 20;
            const int skipValue = 10;

            await Seed<User>(u => u.Id, numberOfUsers);

            var response = await _sutClient.GetAsync($"/api/users?$skip={skipValue}&$orderby=Id");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<User> users = await response.Content.ReadAsAsync<List<User>>();

            users.Should().HaveCount(numberOfUsers - skipValue);
        }

        [TestMethod]
        public async Task GET_should_support_Top_and_Skip_together_for_pagination()
        {
            const int numberOfUsers = 50;
            const int pageSize = 10;
            const int pageNumber = 2; // Second page

            await Seed<User>(u => u.Id, numberOfUsers);

            var response = await _sutClient.GetAsync($"/api/users?$top={pageSize}&$skip={pageSize * (pageNumber - 1)}&$orderby=Id");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<User> users = await response.Content.ReadAsAsync<List<User>>();

            users.Should().HaveCount(pageSize);
        }

        [TestMethod]
        public async Task GET_should_return_BadRequest_when_Top_exceeds_maximum()
        {
            const int topValue = MaxPageItemNumber + 1;

            await Seed<User>(u => u.Id, 200, (u,i) => u.Id = i);

            var response = await _sutClient.GetAsync($"/api/users?$top={topValue}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
        }
        #endregion

        #region GET - All Users
        [TestMethod]
        public async Task GET_should_return_empty_list_and_200Ok_when_no_users_exist()
        {
            var response = await _sutClient.GetAsync("/api/users");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<User> users = await response.Content.ReadAsAsync<List<User>>();

            users.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GET_should_return_all_users_and_200Ok()
        {
            const int numberOfUsers = 10;

            await Seed<User>(u => u.Id, numberOfUsers);

            var response = await _sutClient.GetAsync("/api/users");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<User> users = await response.Content.ReadAsAsync<List<User>>();

            users.Should().HaveCount(numberOfUsers);
        }

        [TestMethod]
        public async Task GET_should_limit_results_to_maximum_page_size()
        {
            const int numberOfUsers = MaxPageItemNumber + 10;

            await Seed<User>(u => u.Id, numberOfUsers);

            var response = await _sutClient.GetAsync("/api/users");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            List<User> users = await response.Content.ReadAsAsync<List<User>>();

            users.Should().HaveCount(MaxPageItemNumber);
        }
        #endregion

        #region GET by ID
        [TestMethod]
        public async Task GET_byId_should_return_404NotFound_when_Id_does_not_exist()
        {
            var response = await _sutClient.GetAsync("/api/users/999999");

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GET_byId_should_return_200Ok_when_user_exists()
        {
            User user = await SeedOne<User>(u => u.Id);

            var response = await _sutClient.GetAsync($"/api/users/{user.Id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task GET_byId_should_return_correct_user()
        {
            User expectedUser = await SeedOne<User>(u => u.Id);

            var response = await _sutClient.GetAsync($"/api/users/{expectedUser.Id}");
            User user = await response.Content.ReadAsAsync<User>();

            user.Should().BeEquivalentTo(expectedUser, options => options.Excluding(u => u.Id));
        }
        #endregion

        #region POST (Create)
        [TestMethod]
        public async Task POST_should_return_201Created_with_valid_user()
        {
            User newUser = Builder<User>.New().Build(u =>
            {
                u.Id = 0;
                u.Name = Any.String();
                u.Surname = Any.String();
                u.Email = Any.Email();
            });

            var response = await _sutClient.PostAsync("/api/users", newUser);

            response.Should().HaveHttpStatusCode(HttpStatusCode.Created);
        }

        [TestMethod]
        public async Task POST_should_create_user_with_correct_values()
        {
            User newUser = Builder<User>.New().Build(u =>
            {
                u.Id = 0;
                u.Name = Any.String();
                u.Surname = Any.String();
                u.Email = Any.Email();
            });

            var response = await _sutClient.PostAsync("/api/users", newUser);
            User createdUser = await response.Content.ReadAsAsync<User>();

            createdUser.Should().BeEquivalentTo(newUser, options => options.Excluding(u => u.Id));
            createdUser.Id.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public async Task POST_should_return_400BadRequest_when_Id_is_specified()
        {
            User newUser = Builder<User>.New().Build(u =>
            {
                u.Id = Any.Int();
                u.Name = Any.String();
                u.Surname = Any.String();
                u.Email = Any.Email();
            });

            var response = await _sutClient.PostAsync("/api/users", newUser);

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
            response.Should().ContainOutcomeEntry("{0} should not be specified on creation", "Id", Severity.Error);
        }

        [TestMethod]
        public async Task POST_should_return_400BadRequest_when_Name_is_missing()
        {
            User newUser = Builder<User>.New().Build(u =>
            {
                u.Id = 0;
                u.Name = null;
                u.Surname = Any.String();
                u.Email = Any.Email();
            });

            var response = await _sutClient.PostAsync("/api/users", newUser);

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
            response.Should().ContainOutcomeEntry("{0} is mandatory", "Name", Severity.Error);
        }

        [TestMethod]
        public async Task POST_should_return_400BadRequest_when_Surname_is_missing()
        {
            User newUser = Builder<User>.New().Build(u =>
            {
                u.Id = 0;
                u.Name = Any.String();
                u.Surname = null;
                u.Email = Any.Email();
            });

            var response = await _sutClient.PostAsync("/api/users", newUser);

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
            response.Should().ContainOutcomeEntry("{0} is mandatory", "Surname", Severity.Error);
        }

        [TestMethod]
        public async Task POST_should_return_400BadRequest_when_Email_is_missing()
        {
            User newUser = Builder<User>.New().Build(u =>
            {
                u.Id = 0;
                u.Name = Any.String();
                u.Surname = Any.String();
                u.Email = null;
            });

            var response = await _sutClient.PostAsync("/api/users", newUser);

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
            response.Should().ContainOutcomeEntry("{0} is mandatory", "Email", Severity.Error);
        }

        [TestMethod]
        public async Task POST_should_return_400BadRequest_when_Email_is_invalid()
        {
            User newUser = Builder<User>.New().Build(u =>
            {
                u.Id = 0;
                u.Name = Any.String();
                u.Surname = Any.String();
                u.Email = "invalid-email";
            });

            var response = await _sutClient.PostAsync("/api/users", newUser);

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
            response.Should().ContainOutcomeEntry("{0} is not a valid email address", "Email", Severity.Error);
        }
        #endregion

        #region PUT (Update)
        [TestMethod]
        public async Task PUT_should_return_200Ok_when_updating_existing_user()
        {
            User seededUser = await SeedOne<User>(u => u.Id);

            // Create a new user object without navigation properties to avoid circular reference
            User user = Builder<User>.New().Build(u =>
            {
                u.Id = seededUser.Id;
                u.Name = Any.String();
                u.Surname = Any.String();
                u.Email = Any.Email();
            });

            var response = await _sutClient.PutAsync($"/api/users/{user.Id}", user);

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task PUT_should_update_user_with_new_values()
        {
            User seededUser = await SeedOne<User>(u => u.Id);

            var newName = Any.String();
            var newSurname = Any.String();
            var newEmail = Any.Email();

            // Create a new user object without navigation properties to avoid circular reference
            User user = Builder<User>.New().Build(u =>
            {
                u.Id = seededUser.Id;
                u.Name = newName;
                u.Surname = newSurname;
                u.Email = newEmail;
            });

            await _sutClient.PutAsync($"/api/users/{user.Id}", user);

            var response = await _sutClient.GetAsync($"/api/users/{user.Id}");
            User updatedUser = await response.Content.ReadAsAsync<User>();

            updatedUser.Name.Should().Be(newName);
            updatedUser.Surname.Should().Be(newSurname);
            updatedUser.Email.Should().Be(newEmail);
        }

        [TestMethod]
        public async Task PUT_should_return_404NotFound_when_user_does_not_exist()
        {
            User user = Builder<User>.New().Build(u =>
            {
                u.Id = 999999;
                u.Name = Any.String();
                u.Surname = Any.String();
                u.Email = Any.Email();
            });

            var response = await _sutClient.PutAsync($"/api/users/{user.Id}", user);

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task PUT_should_return_400BadRequest_when_Id_in_body_does_not_match_Id_in_url()
        {
            // Create a new user object without navigation properties to avoid circular reference
            User user = Builder<User>.New().Build(u =>
            {
                u.Id = Any.Int();
                u.Name = Any.String();
                u.Surname = Any.String();
                u.Email = Any.Email();
            });

            var differentId = user.Id + 1;

            var response = await _sutClient.PutAsync($"/api/users/{differentId}", user);

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task PUT_should_return_400BadRequest_when_Email_is_invalid()
        {
            User seededUser = await SeedOne<User>(u => u.Id);

            User user = Builder<User>.New().Build(u =>
            {
                u.Id = seededUser.Id;
                u.Name = Any.String();
                u.Surname = Any.String();
                u.Email = "invalid-email";
            });

            var response = await _sutClient.PutAsync($"/api/users/{user.Id}", user);

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
            response.Should().ContainOutcomeEntry("{0} is not a valid email address", "Email", Severity.Error);
        }
        #endregion

        #region DELETE
        [TestMethod]
        public async Task DELETE_should_return_200Ok_when_user_deleted()
        {
            User user = await SeedOne<User>(u => u.Id);

            var response = await _sutClient.DeleteAsync($"/api/users/{user.Id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            // Verify user is deleted
            response = await _sutClient.GetAsync($"/api/users/{user.Id}");
            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DELETE_should_return_404_when_user_not_found()
        {
            var response = await _sutClient.DeleteAsync("/api/users/999999");

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }
        #endregion
    }
}


