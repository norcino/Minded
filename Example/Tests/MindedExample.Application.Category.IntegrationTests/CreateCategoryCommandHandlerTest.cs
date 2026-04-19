//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Builder;
//using MindedExample.Tests.Integration.Common;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using MindedExample.Application.Category.Command;
//using FluentAssertions;

//namespace MindedExample.Application.Category.IntegrationTests
//{
//    [TestClass]
//    public class CreateCategoryCommandHandlerTest : BaseServiceIntegrationTest
//    {
//        [TestMethod]
//        public async Task Handler_creates_new_category_with_the_correct_properties()
//        {
//            var category = Builder<MindedExample.Domain.Category>.New().Build(c =>
//            {
//                c.Name = "Test category";
//                c.Description = "Test description";
//                c.Active = true;
//            });

//            var command = new CreateCategoryCommand(category);
//            var response = await mediator.ProcessCommandAsync<int>(command);

//            Assert.IsTrue(response.Successful, "The command response is successful");

//            var createdCategory = await Context.Categories.SingleAsync(p => p.Id == response.Result);

//            category.Id = response.Result;
//            createdCategory.Should().BeEquivalentTo(category);
//        }
//    }
//}
