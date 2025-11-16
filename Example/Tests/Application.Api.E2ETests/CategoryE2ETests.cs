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
    [DoNotParallelize]
    [TestClass]
    public class CategoryE2ETests : BaseE2ETest
    {
        #region Get Skip - Category?$skip={#}
        [TestMethod]
        public async Task GET_using_Skip_Should_omit_undesired_entities()
        {
            const int entitiesToCreate = 120;
            const int numberOfResultsToSkip = 10;

            await Seed<Category>(c => c.Id, entitiesToCreate);

            var response = await _sutClient.GetAsync($"/api/category?$skip={numberOfResultsToSkip}&$orderby=Id");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var categories = await response.Content.ReadAsAsync<List<Category>>();

            categories.Should().HaveCount(MaxPageItemNumber);

            categories.All(c => c.Id >= 11 && c.Id <= numberOfResultsToSkip + MaxPageItemNumber);
        }

        [TestMethod]
        public async Task GET_using_Skip_Should_return_empty_list_when_value_greater_then_Count()
        {
            const int entitiesToCreate = 10;
            const int numberOfResultsToSkip = 10;

            await Seed<Category>(c => c.Id, entitiesToCreate);

            var response = await _sutClient.GetAsync($"/api/category?$skip={numberOfResultsToSkip}&$orderby=Id");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var categories = await response.Content.ReadAsAsync<List<Category>>();
            categories.Should().HaveCount(0);
        }
        #endregion

        #region Get Top - Category?$top={#}
        [TestMethod]
        public async Task GET_using_Top_should_return_BadRequest_when_requested_number_is_higher_than_MaximumNumberOfResults()
        {
            const int numberOfDesiredResults = MaxPageItemNumber * 2;
            await Seed<Category>(c => c.Id);

            var response = await _sutClient.GetAsync($"/api/category?$top={numberOfDesiredResults}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);

            var categories = await response.Content.ReadAsStringAsync();
        }

        [TestMethod]
        public async Task GET_using_Top_should_return_desired_number_when_enough_results_exist()
        {
            int desiredNumber = Any.Int(minValue: 1, maxValue: MaxPageItemNumber);
            await Seed<Category>(c => c.Id, MaxPageItemNumber);

            var response = await _sutClient.GetAsync($"/api/category?$top={desiredNumber}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var categories = await response.Content.ReadAsAsync<List<Category>>();

            categories.Should().HaveCount(desiredNumber);
        }

        [TestMethod]
        public async Task GET_using_Top_should_return_empty_list_when_no_results_available()
        {
            var response = await _sutClient.GetAsync($"/api/category?$top=1");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var categories = await response.Content.ReadAsAsync<List<Category>>();
            categories.Should().BeEmpty();
        }
        #endregion

        #region Get Order by Name - Category?$orderby=Name
        [TestMethod]
        public async Task GET_should_support_OrderBy_Name_descending()
        {
            const int expectedCategories = 5;
            await Seed<Category>(c => c.Id, expectedCategories, (c, i) =>
            {
                c.Active = 1 % 2 == 0;
                c.Name = (expectedCategories - i).ToString();
                c.Description = i.ToString();
            });

            var response = await _sutClient.GetAsync("/api/category?$orderby=Name desc");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var categories = await response.Content.ReadAsAsync<List<Category>>();

            categories.Should().HaveCount(expectedCategories);
            categories.Should().BeInDescendingOrder(c => c.Name);
        }

        [TestMethod]
        public async Task GET_should_support_OrderBy_Name_ascending()
        {
            const int expectedCategories = 5;
            await Seed<Category>(c => c.Id, expectedCategories, (c, i) =>
            {
                c.Active = 1 % 2 == 0;
                c.Name = (expectedCategories - i).ToString();
                c.Description = i.ToString();
            });

            var response = await _sutClient.GetAsync("/api/category?$orderby=Name");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var categories = await response.Content.ReadAsAsync<List<Category>>();

            categories.Should().HaveCount(expectedCategories);
            categories.Should().BeInAscendingOrder(c => c.Name);
        }
        #endregion

        #region Get Filter by Id - Category?$oderby=Id
        [TestMethod]
        public async Task GET_should_support_OrderBy_Id_ascending()
        {
            const int NumberOfTransactionsToCreate = 30;
            await Seed<Category>(c => c.Id, NumberOfTransactionsToCreate);

            var response = await _sutClient.GetAsync("/api/category?$orderby=Id");
            var categories = await response.Content.ReadAsAsync<List<Category>>();

            categories.Should().HaveCount(NumberOfTransactionsToCreate);
            categories.Should().BeInAscendingOrder(c => c.Id);
        }

        [TestMethod]
        public async Task GET_should_support_OrderBy_Id_descending()
        {
            const int NumberOfTransactionsToCreate = 30;
            await Seed<Category>(c => c.Id, NumberOfTransactionsToCreate);

            var response = await _sutClient.GetAsync("/api/category?$orderby=Id desc");
            var categories = await response.Content.ReadAsAsync<List<Category>>();

            categories.Should().HaveCount(NumberOfTransactionsToCreate);
            categories.Should().BeInDescendingOrder(c => c.Id);
        }
        #endregion

        #region Get All - Category
        [TestMethod]
        public async Task GET_should_returns_empty_list_and_200Ok_when_none_exists()
        {
            var response = await _sutClient.GetAsync("/api/category");

            response.Should().NotBeNull();
            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var categories = await response.Content.ReadAsAsync<List<Category>>();
            categories.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GET_should_return_sole_existing_category_and_200Ok_when_only_one_exist()
        {
            var expectedCategories = await Seed<Category>(c => c.Id);

            var response = await _sutClient.GetAsync("/api/category");

            response.Should().NotBeNull();
            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var categories = await response.Content.ReadAsAsync<List<Category>>();
            categories.Count.Should().Be(expectedCategories.Count());

            categories.Should().BeEquivalentTo(expectedCategories, o => o.Excluding(c => c.Id));
        }

        [TestMethod]
        public async Task GET_should_return_all_Categories_and_200Ok()
        {
            var numberOfExistingCategories = 10;
            var expectedCategories = await Seed<Category>(c => c.Id, numberOfExistingCategories);

            var response = await _sutClient.GetAsync("/api/category");

            response.Should().NotBeNull();
            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            var categories = await response.Content.ReadAsAsync<List<Category>>();
            categories.Count.Should().Be(expectedCategories.Count());

            categories.Should().BeEquivalentTo(expectedCategories, o => o.Excluding(c => c.Id));
        }

        [TestMethod]
        public async Task GET_return_all_categories_limiting_to_the_first_100()
        {
            const int NumberOfCatetoriesToCreate = 110;

            await Seed<Category>(c => c.Id, NumberOfCatetoriesToCreate);

            var response = await _sutClient.GetAsync("/api/category");

            var categories = await response.Content.ReadAsAsync<List<Category>>();
            categories.Should().HaveCount(MaxPageItemNumber);
        }
        #endregion

        #region Get by ID Category/{id}
        [TestMethod]
        public async Task GET_byId_should_return_404NotFound_when_Id_does_not_exist()
        {
            var response = await _sutClient.GetAsync("/api/category/999");

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GET_byId_should_return_200Ok_when_entity_when_specified_Id_exists()
        {
            var category = await SeedOne<Category>(c => c.Id);
            var response = await _sutClient.GetAsync($"/api/category/{category.Id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task GET_byId_should_return_correct_and_complete_entity_when_the_specified_Id_exists()
        {
            var expectedCategory = await SeedOne<Category>(c => c.Id);
            var response = await _sutClient.GetAsync($"/api/category/{expectedCategory.Id}");
            var category = await response.Content.ReadAsAsync<Category>();

            category.Should().BeEquivalentTo(expectedCategory, o => o.Excluding(c => c.Id));
        }
        #endregion

        #region Post (Create) - Category
        [TestMethod]
        public async Task POST_should_return_201Created_passing_valid_entity()
        {
            var expectedCategory = Builder<Category>.New().Build(c => c.Id = 0);
            var response = await _sutClient.PostAsync("/api/category", expectedCategory);

            response.Should().HaveHttpStatusCode(HttpStatusCode.Created);
        }

        [TestMethod]
        public async Task POST_creates_valid_entity()
        {
            var expectedCategory = Builder<Category>.New().Build(c => c.Id = 0);
            var response = await _sutClient.PostAsync("/api/Category", expectedCategory);

            var category = await response.Content.ReadAsAsync<Category>();

            category.Should().BeEquivalentTo(expectedCategory, o => o.Excluding(c => c.Id));
        }

        [TestMethod]
        public async Task POST_should_return_BadRequest_400BadRequest_when_entity_contains_Id()
        {
            var expectedCategory = Builder<Category>.New().Build(c => c.Id = Any.Int());

            var response = await _sutClient.PostAsync("/api/Category", expectedCategory);

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
            response.Should().ContainOutcomeEntry("{0} should not be specified on creation", "Id", Severity.Error);
        }
        #endregion

        #region Delete - Category/{id}
        [TestMethod]
        public async Task DELETE_should_return_200Ok_when_category_deleted()
        {
            var expectedCategory = await SeedOne<Category>(c => c.Id);
            var response = await _sutClient.DeleteAsync($"/api/category/{expectedCategory.Id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);

            response = await _sutClient.GetAsync($"/api/category/{expectedCategory.Id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DELETE_should_return_404Ok_when_category_not_found()
        {
            var id = Any.Int();
            var response = await _sutClient.DeleteAsync($"/api/Category/{id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);

            response = await _sutClient.GetAsync($"/api/category/{id}");

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }
        #endregion

        #region Put (Update) - Category/{id}
        [TestMethod]
        public async Task PUT_should_return_200Ok_when_updating_existing_category()
        {
            var category = await SeedOne<Category>(c => c.Id);
            category.Name = Any.String();
            category.Description = Any.String();

            var response = await _sutClient.PutAsync($"/api/category/{category.Id}", category);

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task PUT_should_update_category_with_new_values()
        {
            var category = await SeedOne<Category>(c => c.Id);
            var newName = Any.String();
            var newDescription = Any.String();

            category.Name = newName;
            category.Description = newDescription;

            await _sutClient.PutAsync($"/api/category/{category.Id}", category);

            var response = await _sutClient.GetAsync($"/api/category/{category.Id}");
            var updatedCategory = await response.Content.ReadAsAsync<Category>();

            updatedCategory.Name.Should().Be(newName);
            updatedCategory.Description.Should().Be(newDescription);
        }

        [TestMethod]
        public async Task PUT_should_return_404NotFound_when_category_does_not_exist()
        {
            var category = Builder<Category>.New().Build(c => c.Id = Any.Int());

            var response = await _sutClient.PutAsync($"/api/category/{category.Id}", category);

            response.Should().HaveHttpStatusCode(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task PUT_should_return_400BadRequest_when_Id_in_body_does_not_match_Id_in_url()
        {
            var category = await SeedOne<Category>(c => c.Id);
            var differentId = category.Id + 1;

            var response = await _sutClient.PutAsync($"/api/category/{differentId}", category);

            response.Should().HaveHttpStatusCode(HttpStatusCode.BadRequest);
        }
        #endregion

        #region Get Filter - Category?$filter=
        [TestMethod]
        public async Task GET_should_support_filter_by_Name()
        {
            const int numberOfCategories = 10;
            var targetName = "Electronics";

            await Seed<Category>(c => c.Id, numberOfCategories, (c, i) =>
            {
                c.Name = i == 5 ? targetName : Any.String();
            });

            var response = await _sutClient.GetAsync($"/api/category?$filter=Name eq '{targetName}'");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var categories = await response.Content.ReadAsAsync<List<Category>>();

            categories.Should().HaveCount(1);
            categories.First().Name.Should().Be(targetName);
        }

        [TestMethod]
        public async Task GET_should_support_filter_by_Active_status()
        {
            const int numberOfCategories = 10;

            await Seed<Category>(c => c.Id, numberOfCategories, (c, i) =>
            {
                c.Active = i % 2 == 0;
            });

            var response = await _sutClient.GetAsync("/api/category?$filter=Active eq true");

            response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
            var categories = await response.Content.ReadAsAsync<List<Category>>();

            categories.Should().HaveCount(5);
            categories.Should().OnlyContain(c => c.Active);
        }
        #endregion

        //#region Get Count - Category?$count=true
        //[TestMethod]
        //public async Task GET_should_support_count_parameter()
        //{
        //    const int numberOfCategories = 25;
        //    await Seed<Category>(c => c.Id, numberOfCategories);

        //    var response = await _sutClient.GetAsync("/api/category?$count=true");

        //    response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        //    var content = await response.Content.ReadAsStringAsync();

        //    // OData count response includes @odata.count property
        //    content.Should().Contain("@odata.count");
        //    content.Should().Contain($"\"@odata.count\":{numberOfCategories}");
        //}
        //#endregion

        //#region Get Select - Category?$select=
        //[TestMethod]
        //public async Task GET_should_support_select_specific_properties()
        //{
        //    await Seed<Category>(c => c.Id, 5);

        //    var response = await _sutClient.GetAsync("/api/category?$select=Id,Name");

        //    response.Should().HaveHttpStatusCode(HttpStatusCode.OK);
        //    var content = await response.Content.ReadAsStringAsync();

        //    // Should contain Id and Name
        //    content.Should().Contain("\"Id\"");
        //    content.Should().Contain("\"Name\"");

        //    // Should NOT contain Description or Active
        //    content.Should().NotContain("\"Description\"");
        //    content.Should().NotContain("\"Active\"");
        //}
        //#endregion
    }
}
