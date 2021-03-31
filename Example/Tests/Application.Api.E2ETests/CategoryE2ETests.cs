using System;
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

namespace Application.Api.IntegrationTests
{
    [TestClass]
    public class CategoryE2ETests : BaseE2ETest
    {
        [TestMethod]
        public async Task Get_all_Categories_should_return_200Ok_and_All_existing_categories()
        {
            var expectedCategories = Seed<Category>(c => c.Id);

            var response = await _sutClient.GetAsync("/api/category");

            response.Should().NotBeNull();
            response.IsSuccessStatusCode.Should().BeTrue();

            var categories = await response.Content.ReadAsAsync<List<Category>>();
            Assert.AreEqual(expectedCategories.Count(), categories.Count);

            categories.Should().BeEquivalentTo(expectedCategories, o => o.Excluding(c => c.Id));
        }

        #region Get Skip - Category?$skip={#}
        [TestMethod]
        public async Task GET_using_Skip_Should_omit_top_undesired_entities()
        {
            const int entitiesToCreate = 200;
            const int numberOfResultsToSkip = 10;

            Seed<Category>(c => c.Id, entitiesToCreate);

            var response = await _sutClient.GetAsync($"/api/category?$skip={numberOfResultsToSkip}&$orderby=Id");
            
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var categories = await response.Content.ReadAsAsync<List<Category>>();

            categories.Should().HaveCount(MaxPageItemNumber);

            categories.All(c => c.Id >= 11 && c.Id <= numberOfResultsToSkip + MaxPageItemNumber);
        }

        [TestMethod]
        public async Task GET_using_Skip_with_value_greater_then_Count_Should_return_empty_list()
        {
            const int entitiesToCreate = 10;
            const int numberOfResultsToSkip = 10;

            Seed<Category>(c => c.Id, entitiesToCreate);

            var response = await _sutClient.GetAsync($"/api/category?$skip={numberOfResultsToSkip}&$orderby=Id");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var categories = await response .Content.ReadAsAsync<List<Category>>();
            categories.Should().HaveCount(0);
        }
        #endregion

        #region Get Top - Category?$top={#}
        [TestMethod]
        public async Task GET_using_Top_with_value_greater_then_MaximumNumberOfResults_Should_return_MaximumNumberOfResults()
        {
            const int entitiesToCreate = MaxPageItemNumber * 2;
            const int numberOfDesiredResults = MaxPageItemNumber * 2;
            Seed<Category>(c => c.Id);

            var response = await _sutClient.GetAsync($"/api/category?$top={numberOfDesiredResults}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var categories = await response .Content.ReadAsAsync<List<Category>>();
            categories.Should().HaveCount(MaxPageItemNumber);
        }

        [TestMethod]
        public async Task GET_using_Top_with_no_results_empty_list()
        {
            var response = await _sutClient.GetAsync($"/api/category?$top=1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var categories = await response .Content.ReadAsAsync<List<Category>>();
            categories.Should().BeEmpty();
        }
        #endregion

        #region Get Filter by Name - Category?$orderby=Name
        //[TestMethod]
        //public async Task GET_support_OrderBy_Name_descending()
        //{
        //    const int expectedCategories = 5;
        //    Seed<Category>(c => c.Id, expectedCategories, (c, i) =>
        //    {
        //        c.Active = 1 % 2 == 0;
        //        c.Name = (expectedCategories - i).ToString();
        //        c.Description = i.ToString();
        //    });

        //    var response = await _sutClient.GetAsync("/api/category?$orderby=Name desc");

        //    response.StatusCode.Should().Be(HttpStatusCode.OK);
        //    var categories = await response.Content.ReadAsAsync<List<Category>>();

        //    categories.Should().HaveCount(expectedCategories);
        //    categories.Should().BeInDescendingOrder(c => c.Name);
        //}

        //[TestMethod]
        //public async Task GET_support_OrderBy_Name_ascending()
        //{
        //    const int expectedCategories = 5;
        //    Seed<Category>(c => c.Id, expectedCategories, (c, i) =>
        //    {
        //        c.Active = 1 % 2 == 0;
        //        c.Name = (expectedCategories - i).ToString();
        //        c.Description = i.ToString();
        //    });

        //    var response = await _sutClient.GetAsync("/api/category?$orderby=Name");

        //    response.StatusCode.Should().Be(HttpStatusCode.OK);
        //    var categories = await response.Content.ReadAsAsync<List<Category>>();
        //    for (var i = 0; i < expectedCategories - 1; i++)
        //    {
        //        Assert.IsTrue(categories[i].Id > categories[i + 1].Id);
        //        Assert.IsTrue(int.Parse(categories[i].Name) < int.Parse(categories[i + 1].Name));
        //    }
        //}
        #endregion

        #region Get Filter by Id - Category?$oderby=Id
        //[TestMethod]
        //public async Task GET_support_OrderBy_Id_ascending()
        //{
        //    const int NumberOfTransactionsToCreate = 3;
        //    Seed<Category>(c => c.Id, NumberOfTransactionsToCreate);

        //    var response = await _sutClient.GetAsync("/api/category?&orderby=Id");
        //    var categories = await response .Content.ReadAsAsync<List<Category>>();

        //    categories.Should().HaveCount(NumberOfTransactionsToCreate);
        //    categories.Should().BeInAscendingOrder(c => c.Id);
        //}

        //[TestMethod]
        //public async Task GET_support_OrderBy_Id_descending()
        //{
        //    const int NumberOfTransactionsToCreate = 3;
        //    Seed<Category>(c => c.Id, NumberOfTransactionsToCreate);

        //    var response = await _sutClient.GetAsync("/api/category?$orderby=Id desc");
        //    var categories = await response .Content.ReadAsAsync<List<Category>>();

        //    categories.Should().HaveCount(NumberOfTransactionsToCreate);
        //    categories.Should().BeInDescendingOrder(c => c.Id);
        //}
        #endregion

        #region Get All - Category
        [TestMethod]
        public async Task GET_return_all_categories()
        {
            var numberOfExistingCategories = 10;
            Seed<Category>(c => c.Id, numberOfExistingCategories);

            var response = await _sutClient.GetAsync("/api/category");

            var categories = await response.Content.ReadAsAsync<List<Category>>();
            categories.Should().HaveCount(numberOfExistingCategories);
        }

        [TestMethod]
        public async Task GET_return_all_categories_limiting_to_the_first_100()
        {
            const int NumberOfCatetoriesToCreate = 110;

            Seed<Category>(c => c.Id, NumberOfCatetoriesToCreate);

            var response = await _sutClient.GetAsync("/api/category");

            var categories = await response.Content.ReadAsAsync<List<Category>>();
            categories.Should().HaveCount(MaxPageItemNumber);
        }
        #endregion

        #region Get by ID Category/{id}
        [TestMethod]
        public async Task GET_byId_returns_404_when_id_does_not_exist()
        {
            var response = await _sutClient.GetAsync("/api/category/1");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GET_byId_returns_ok_200_when_entity_with_specified_Id_exists()
        {
            var category = SeedOne<Category>(c => c.Id);
            var response = await _sutClient.GetAsync($"/api/category/{category.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task GET_byId_returns_correct_and_complete_entity_with_the_specified_Id()
        {
            var expectedCategory = SeedOne<Category>(c => c.Id);
            var response = await _sutClient.GetAsync($"/api/category/{expectedCategory.Id}");
            var category = await response.Content.ReadAsAsync<Category>();

            category.Should().BeEquivalentTo(expectedCategory, o => o.Excluding(c => c.Id));
        }
        #endregion

        #region POST - Category
        [TestMethod]
        public async Task POST_returns_201_passing_valid_entity()
        {
            var expectedCategory = Builder<Category>.New().Build(c => c.Id = 0);
            var response = await _sutClient.PostAsync("/api/category", expectedCategory);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [TestMethod]
        public async Task POST_location_URI_to_access_the_created_entity()
        {
            var expectedCategory = Builder<Category>.New().Build(c => c.Id = 0);
            var postResponse = await _sutClient.PostAsync<Category>("/api/Category", expectedCategory);
            Assert.IsNotNull(postResponse.Headers.Location.AbsoluteUri, "Location should be set with the URL to the created object");
            Assert.IsTrue(postResponse.Headers.Location.AbsoluteUri.Contains("/api/Category/"), "Entity URI has targets the right position");
        }

        [TestMethod]
        public async Task POST_creates_valid_entity()
        {
            var expectedCategory = Builder<Category>.New().Build(c => c.Id = 0);
            var postResponse = await _sutClient.PostAsync<Category>("/api/Category", expectedCategory);
            var getResponse = await _sutClient.GetAsync(postResponse.Headers.Location.AbsoluteUri);
            
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var category = await getResponse.Content.ReadAsAsync<Category>();
            
            category.Should().BeEquivalentTo(expectedCategory, o => o.Excluding(c => c.Id));
        }

        [TestMethod]
        public async Task POST_return_BadRequest_400_trying_to_create_entity_with_Id()
        {
            var expectedCategory = Builder<Category>.New().Build(c => c.Id = Any.Int());

            var response = await _sutClient.PostAsync("/api/Category", expectedCategory);
            
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        #endregion
    }
}
