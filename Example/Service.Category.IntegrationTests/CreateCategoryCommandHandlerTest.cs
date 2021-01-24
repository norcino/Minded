using System;
using System.Linq;
using System.Threading.Tasks;
using Builder;
using Common.IntegrationTests;
using FluentAssertion.MSTest;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Service.Category.Command;

namespace Service.Category.IntegrationTests
{
    [TestClass]
    public class CreateCategoryCommandHandlerTest : BaseServiceIntegrationTest
    {
        [TestMethod]
        public async Task Handler_creates_new_category_with_the_correct_properties()
        {
            var category = Builder<Data.Entity.Category>.New().Build(c => {
                c.Id = 0;
                c.Name = "Test category";
                c.Description = "Test description";
                c.Active = true;
            });

            var command = new CreateCategoryCommand(category);
            var response = await mediator.ProcessCommandAsync<int>(command);

            Assert.IsTrue(response.Successful, "The command response is successful");
           
            var createdCategory = await Context.Categories.SingleAsync(p => p.Id == response.Result);
            
            Assert.That.This(createdCategory).HasSameProperties(category, "Id");
            Assert.That.This(Context.Categories.Any()).IsTrue();
        }
    }
}
