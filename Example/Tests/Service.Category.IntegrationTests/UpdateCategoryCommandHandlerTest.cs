//using System.Threading.Tasks;
//using Common.IntegrationTests;
//using Common.Tests.FluentAssertion;
//using Data.Common.Testing.Builder;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Service.Category.Command;

//namespace Service.Category.IntegrationTests
//{
//    [TestClass]
//    public class UpdateCategoryCommandHandlerTest : BaseServiceIntegrationTest
//    {
//        [TestMethod]
//        public async Task Handler_update_new_category_with_the_correct_properties()
//        {
//            var category = Persister<Data.Entity.Category>.New().Persist();

//            category.Active = !category.Active;
//            category.Name = category.Name + "2";
//            category.Description = category.Description + "2";

//            var command = new UpdateCategoryCommand(category.Id, category);
//            var response = await mediator.ProcessCommandAsync<Data.Entity.Category>(command);

//            Assert.IsTrue(response.Successful, "The command response is successful");

//            var updatedCategory = await Context.Categories.SingleAsync(p => p.Id == response.Result.Id);

//            Assert.That.This(updatedCategory).HasSameProperties(category);
//        }
//    }
//}
