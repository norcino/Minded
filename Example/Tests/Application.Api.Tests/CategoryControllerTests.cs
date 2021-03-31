using System.Collections.Generic;
using Application.Api.Controllers;
using Data.Entity;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.OData.Edm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Mediator;
using Moq;
using Service.Category.Command;
using Service.Category.Query;
using Builder;
using AnonymousData;

namespace Application.Api.Tests
{
    [TestClass]
    public class CategoryControllerTests
    {
        private CategoryController _controller;
        private Mock<IMediator> _mediatorMock;
        private Builder<Category> _builder;

        [TestInitialize]
        public void TestInitialize()
        {
            _builder = Builder<Category>.New();
            _mediatorMock = new Mock<IMediator>();
            _controller = new CategoryController(_mediatorMock.Object);
        }

        // TODO in generic test base for the controller, make sure Ordering, Filtering, Expansion and pagination is all supported

//        public Task<List<Category>> Get(ODataQueryOptions<Category> queryOptions)
//        {
//            var query = ApplyODataQueryConditions<Category, GetCategoriesQuery>(queryOptions, new GetCategoriesQuery());
//            return _mediator.ProcessQueryAsync(query);
//        }

        //[TestMethod]
        //public void Get_invokes_ProcessQueryAsync_on_mediator_passing_GetCategoriesQuery_with_correct_data()
        //{
        //    var queryOptions = new ODataQueryOptions<Category>(new ODataQueryContext(new EdmModel(), typeof(Category), new ODataPath()), new DefaultHttpRequest(new DefaultHttpContext()));
        //   // ApplyODataQueryConditions<Category, GetCategoriesQuery>(queryOptions, new GetCategoriesQuery());

        //    var categoryId = AnonymousData.Int();
        //    var response = _controller.Get(categoryId);
        //    _mediatorMock.Verify(sm => sm.ProcessQueryAsync(It.Is<GetCategoriesQuery>(c =>
        //        c.Top != null
        //    )), Times.Once);
        //}

        [TestMethod]
        public void GetById_invokes_ProcessQueryAsync_on_mediator_passing_GetCategoryByIdQuery_with_correct_data()
        {
            var categoryId = Any.Int();
            var response = _controller.Get(categoryId);

            _mediatorMock.Verify(sm => sm.ProcessQueryAsync(It.Is<GetCategoryByIdQuery>(c =>
                c.CategoryId == categoryId
            )), Times.Once);
        }
        
        [TestMethod]
        public void Post_invokes_ProcessCommandAsync_on_mediator_passing_CreateCategoryCommand_with_correct_data()
        {
            var category = _builder.Build(c => c.Id = 0);
            var response = _controller.PostAsync(category);

            _mediatorMock.Verify(sm => sm.ProcessCommandAsync<int>(It.Is<CreateCategoryCommand>(c =>
                c.Category.Description == category.Description &&
                c.Category.Name == category.Name &&
                c.Category.Active == category.Active
            )), Times.Once);
        }
    }
}
