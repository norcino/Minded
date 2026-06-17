using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MindedExample.Application.User.Query;
using MindedExample.Application.User.QueryHandler;
using MindedExample.Infrastructure.Persistence;
using QM.Common.Testing;

namespace MindedExample.Application.User.UnitTests
{
    /// <summary>
    /// Unit tests for ExistsUserInCurrentTenantQueryHandler.
    /// Tests tenant-scoped user existence checks using mocked context and user accessor.
    /// </summary>
    [TestClass]
    public class ExistsUserInCurrentTenantQueryHandlerTest
    {
        private const int TestTenantId = 5;

        private ExistsUserInCurrentTenantQueryHandler _sut;
        private Mock<IMindedExampleContext> _contextMock;
        private Mock<ICurrentUserAccessor> _currentUserAccessorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _contextMock = new Mock<IMindedExampleContext>();
            _currentUserAccessorMock = new Mock<ICurrentUserAccessor>();
            _currentUserAccessorMock.SetupGet(a => a.TenantId).Returns(TestTenantId);
            _sut = new ExistsUserInCurrentTenantQueryHandler(_contextMock.Object, _currentUserAccessorMock.Object);
        }

        /// <summary>
        /// Verifies that the handler returns true when the user exists in the current tenant.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_ReturnsTrue_WhenUserExistsInCurrentTenant()
        {
            var userId = 10;
            var users = new List<MindedExample.Domain.User>
            {
                new MindedExample.Domain.User { Id = userId, TenantId = TestTenantId }
            }.GetMockDbSet();
            _contextMock.Setup(c => c.Users).Returns(users.Object);

            var result = await _sut.HandleAsync(new ExistsUserInCurrentTenantQuery(userId));

            result.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that the handler returns false when the user does not exist in the current tenant.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_ReturnsFalse_WhenUserDoesNotExistInCurrentTenant()
        {
            var users = new List<MindedExample.Domain.User>().GetMockDbSet();
            _contextMock.Setup(c => c.Users).Returns(users.Object);

            var result = await _sut.HandleAsync(new ExistsUserInCurrentTenantQuery(999));

            result.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that the handler returns false when user exists but belongs to a different tenant.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_ReturnsFalse_WhenUserBelongsToDifferentTenant()
        {
            var userId = 10;
            var users = new List<MindedExample.Domain.User>
            {
                new MindedExample.Domain.User { Id = userId, TenantId = 999 }
            }.GetMockDbSet();
            _contextMock.Setup(c => c.Users).Returns(users.Object);

            var result = await _sut.HandleAsync(new ExistsUserInCurrentTenantQuery(userId));

            result.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that the handler returns false when there is no current tenant (unauthenticated).
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_ReturnsFalse_WhenNoCurrentTenant()
        {
            _currentUserAccessorMock.SetupGet(a => a.TenantId).Returns((int?)null);
            var users = new List<MindedExample.Domain.User>().GetMockDbSet();
            _contextMock.Setup(c => c.Users).Returns(users.Object);

            var result = await _sut.HandleAsync(new ExistsUserInCurrentTenantQuery(10));

            result.Should().BeFalse();
        }
    }
}
