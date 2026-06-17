using System.Threading;
using System.Threading.Tasks;
using Minded.Extensions.WebApi;
using MindedExample.Api.Controllers;
using MindedExample.Api.Models;
using MindedExample.Application.Configuration.Command;
using MindedExample.Application.Configuration.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace MindedExample.Api.UnitTests
{
    [TestClass]
    public class TenantsControllerTests
    {
        private TenantsController _controller;
        private Mock<IRestMediator> _restMediatorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _restMediatorMock = new Mock<IRestMediator>();
            _controller = new TenantsController(_restMediatorMock.Object);
        }

        [TestMethod]
        public async Task GetTenants_invokes_rest_mediator_with_GetMany_and_GetAdminTenantSummariesQuery()
        {
            var cancellationToken = new CancellationToken();
            _restMediatorMock
                .Setup(m => m.ProcessRestQueryAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<GetAdminTenantSummariesQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            await _controller.GetTenants(cancellationToken);

            _restMediatorMock.Verify(m => m.ProcessRestQueryAsync(
                RestOperation.GetMany,
                It.IsAny<GetAdminTenantSummariesQuery>(),
                cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task CreateTenant_invokes_rest_mediator_with_CreateWithContent_and_CreateTenantCommand()
        {
            var request = new CreateTenantRequest
            {
                Name = "Tenant A",
                LegalOwnerName = "Owner",
                LegalOwnerSurname = "One",
                LegalOwnerEmail = "owner@example.com",
                LegalOwnerPassword = "P@ssw0rd"
            };
            var cancellationToken = new CancellationToken();

            _restMediatorMock
                .Setup(m => m.ProcessRestCommandAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<CreateTenantCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            await _controller.CreateTenant(request, cancellationToken);

            _restMediatorMock.Verify(m => m.ProcessRestCommandAsync(
                RestOperation.CreateWithContent,
                It.Is<CreateTenantCommand>(c =>
                    c.Name == request.Name &&
                    c.LegalOwnerEmail == request.LegalOwnerEmail),
                cancellationToken),
                Times.Once);
        }

        [TestMethod]
        public async Task DeleteTenant_invokes_rest_mediator_with_Delete_and_DeleteTenantCommand()
        {
            const int tenantId = 11;
            var request = new DeleteTenantRequest { ConfirmationName = "Tenant A" };
            var cancellationToken = new CancellationToken();

            _restMediatorMock
                .Setup(m => m.ProcessRestCommandAsync(
                    It.IsAny<RestOperation>(),
                    It.IsAny<DeleteTenantCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OkResult());

            await _controller.DeleteTenant(tenantId, request, cancellationToken);

            _restMediatorMock.Verify(m => m.ProcessRestCommandAsync(
                RestOperation.Delete,
                It.Is<DeleteTenantCommand>(c =>
                    c.TenantId == tenantId &&
                    c.ConfirmationName == request.ConfirmationName),
                cancellationToken),
                Times.Once);
        }
    }
}
