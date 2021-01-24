using System;
using System.Globalization;
using System.Threading.Tasks;
using Builder;
using Common.IntegrationTests;
using FluentAssertion.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Service.Category.Query;

namespace Service.Category.IntegrationTests
{
    [TestClass]
    public class CategoryValidatorTest : BaseServiceIntegrationTest
    {
        [TestMethod]
        public async Task Handler_get_category_by_id_with_the_correct_properties()
        {
            var category = Builder<Data.Entity.Category>.New().Build(e => e.Id = 0);

            await Context.Categories.AddAsync(category);
            await Context.SaveChangesAsync();

            var query = new GetCategoryByIdQuery(category.Id);
            var dbCategory = await mediator.ProcessQueryAsync(query);

            Assert.IsNotNull(dbCategory);
            Assert.That.This(dbCategory).HasSameProperties(category);
        }
    }
}
