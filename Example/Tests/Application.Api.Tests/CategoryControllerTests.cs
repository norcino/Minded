using System.Threading;
using System.Threading.Tasks;
using Application.Api.Controllers;
using Data.Entity;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Service.Category.Command;
using Service.Category.Query;
using AnonymousData;
using Minded.Extensions.WebApi;

namespace Application.Api.Tests
{
    /// <summary>
    /// Unit tests for CategoryController.
    /// Tests verify that controller methods correctly construct commands/queries and pass them to RestMediator.
    /// </summary>
    [TestClass]
    public class CategoryControllerTests
    {
        private CategoryController _controller;
        private Mock<IRestMediator> _mediatorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _mediatorMock = new Mock<IRestMediator>();
            _controller = new CategoryController(_mediatorMock.Object);
        }

        // Note: Testing CategoryController.Get(ODataQueryOptions) is complex because:
        // 1. The controller calls query.ApplyODataQueryOptions(queryOptions) which requires a real ODataQueryOptions object
        // 2. ODataQueryOptions requires ODataQueryContext which requires EdmModel setup
        // 3. This is better tested through integration tests where real OData requests are made
        //
        // The controller's responsibility is to:
        // - Create GetCategoriesQuery
        // - Apply OData options to the query
        // - Call RestMediator with RestOperation.GetMany
        //
        // This is tested indirectly through integration tests.

        [TestMethod]
        public async Task Get_by_id_invokes_ProcessRestQueryAsync_with_GetSingle_operation_and_GetCategoryByIdQuery_with_correct_id()
        {
            // Arrange
            var categoryId = Any.Int();
            var cancellationToken = new CancellationToken();

            _mediatorMock
                .Setup(m => m.ProcessRestQueryAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<GetCategoryByIdQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            // Act
            await _controller.Get(categoryId, cancellationToken);

            // Assert
            _mediatorMock.Verify(m => m.ProcessRestQueryAsync(
                RestOperation.GetSingle,
                It.Is<GetCategoryByIdQuery>(q => q.CategoryId == categoryId),
                cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task Post_invokes_ProcessRestCommandAsync_with_CreateWithContent_operation_and_CreateCategoryCommand_with_correct_category()
        {
            // Arrange
            var category = new Category
            {
                Id = Any.Int(),
                Name = Any.String(),
                Description = Any.String()
            };
            var cancellationToken = new CancellationToken();

            _mediatorMock
                .Setup(m => m.ProcessRestCommandAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<CreateCategoryCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            // Act
            await _controller.Post(category, cancellationToken);

            // Assert
            _mediatorMock.Verify(m => m.ProcessRestCommandAsync(
                RestOperation.CreateWithContent,
                It.Is<CreateCategoryCommand>(c => c.Category == category),
                cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task Put_invokes_ProcessRestCommandAsync_with_UpdateWithContent_operation_and_UpdateCategoryCommand_with_correct_id_and_category()
        {
            // Arrange
            var categoryId = Any.Int();
            var category = new Category
            {
                Id = Any.Int(),
                Name = Any.String(),
                Description = Any.String()
            };
            var cancellationToken = new CancellationToken();

            _mediatorMock
                .Setup(m => m.ProcessRestCommandAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<UpdateCategoryCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            // Act
            await _controller.Put(categoryId, category, cancellationToken);

            // Assert
            _mediatorMock.Verify(m => m.ProcessRestCommandAsync(
                RestOperation.UpdateWithContent,
                It.Is<UpdateCategoryCommand>(c => c.CategoryId == categoryId && c.Category == category),
                cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task Delete_invokes_ProcessRestCommandAsync_with_Delete_operation_and_DeleteCategoryCommand_with_correct_id()
        {
            // Arrange
            var categoryId = Any.Int();
            var cancellationToken = new CancellationToken();

            _mediatorMock
                .Setup(m => m.ProcessRestCommandAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<DeleteCategoryCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            // Act
            await _controller.Delete(categoryId, cancellationToken);

            // Assert
            _mediatorMock.Verify(m => m.ProcessRestCommandAsync(
                RestOperation.Delete,
                It.Is<DeleteCategoryCommand>(c => c.CategoryId == categoryId),
                cancellationToken),
                Times.Once);
        }
    }
}
