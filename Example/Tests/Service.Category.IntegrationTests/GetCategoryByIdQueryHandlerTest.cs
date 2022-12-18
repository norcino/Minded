//using System;
//using System.Globalization;
//using System.Threading.Tasks;
//using Builder;
//using FluentAssertions;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

//namespace Service.Category.IntegrationTests
//{
//    [TestClass]
//    public class CategoryValidatorTest : BaseServiceIntegrationTest
//    {
//        [TestMethod]
//        public async Task Handler_get_category_by_id_with_the_correct_properties()
//        {
//            var category = Builder<Data.Entity.Category>.New().Exclude(c => c.Id).Build();

//            await Context.Categories.AddAsync(category);
//            await Context.SaveChangesAsync();

//            var query = new GetCategoryByIdQuery(category.Id);
//            var dbCategory = await mediator.ProcessQueryAsync(query);

//            dbCategory.Should().BeEquivalentTo(category);
//        }
//    }
//}
