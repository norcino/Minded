using System.Threading;
using System.Threading.Tasks;
using AnonymousData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.WebApi;
using MindedExample.Api.Controllers;
using MindedExample.Application.User.Command;
using MindedExample.Application.User.Query;
using MindedExample.Domain;
using Moq;

namespace MindedExample.Api.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="UsersController"/>.
    /// Verifies that each action correctly constructs its command/query and delegates to IRestMediator
    /// with the right <see cref="RestOperation"/>.
    /// Authorization is handled by the TenantMemberManagement policy and is not tested here.
    /// </summary>
    [TestClass]
    public class UsersControllerTests
    {
        private UsersController _controller;
        private Mock<IRestMediator> _restMediatorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _restMediatorMock = new Mock<IRestMediator>();
            _controller = new UsersController(_restMediatorMock.Object);
        }

        [TestMethod]
        public async Task Get_by_id_invokes_ProcessRestQueryAsync_with_GetSingle_and_GetUserByIdQuery_with_correct_id()
        {
            var userId = Any.Int();
            var cancellationToken = new CancellationToken();

            _restMediatorMock
                .Setup(m => m.ProcessRestQueryAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<GetUserByIdQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            await _controller.Get(userId, cancellationToken);

            _restMediatorMock.Verify(m => m.ProcessRestQueryAsync(
                RestOperation.GetSingle,
                It.Is<GetUserByIdQuery>(q => q.UserId == userId),
                cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task Post_invokes_ProcessRestCommandAsync_with_CreateWithContent_and_CreateUserCommand_with_correct_user()
        {
            var user = new User { Id = Any.Int(), Name = Any.String(), Email = Any.String() };
            var cancellationToken = new CancellationToken();

            _restMediatorMock
                .Setup(m => m.ProcessRestCommandAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<CreateUserCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            await _controller.Post(user, cancellationToken);

            _restMediatorMock.Verify(m => m.ProcessRestCommandAsync(
                RestOperation.CreateWithContent,
                It.Is<CreateUserCommand>(c => c.User == user),
                cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task Put_invokes_ProcessRestCommandAsync_with_UpdateWithContent_and_UpdateUserCommand_with_correct_id_and_user()
        {
            var userId = Any.Int();
            var user = new User { Id = Any.Int(), Name = Any.String() };
            var cancellationToken = new CancellationToken();

            _restMediatorMock
                .Setup(m => m.ProcessRestCommandAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<UpdateUserCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            await _controller.Put(userId, user, cancellationToken);

            _restMediatorMock.Verify(m => m.ProcessRestCommandAsync(
                RestOperation.UpdateWithContent,
                It.Is<UpdateUserCommand>(c => c.UserId == userId && c.User == user),
                cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task Delete_invokes_ProcessRestCommandAsync_with_Delete_and_DeleteUserCommand_with_correct_id()
        {
            var userId = Any.Int();
            var cancellationToken = new CancellationToken();

            _restMediatorMock
                .Setup(m => m.ProcessRestCommandAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<DeleteUserCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            await _controller.Delete(userId, cancellationToken);

            _restMediatorMock.Verify(m => m.ProcessRestCommandAsync(
                RestOperation.Delete,
                It.Is<DeleteUserCommand>(c => c.UserId == userId),
                cancellationToken),
                Times.Once);
        }
    }
}
