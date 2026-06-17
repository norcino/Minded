using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnonymousData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Framework.CQRS.Query;
using MockQueryable.Moq;
using Moq;
using MindedExample.Application.Category.Query;
using MindedExample.Application.Category.QueryHandler;
using MindedExample.Infrastructure.Persistence;
using QM.Common.Testing;

namespace MindedExample.Application.Category.UnitTests
{
    /// <summary>
    /// Unit tests for GetCategoriesQueryHandler.
    /// Verifies tenant filtering and empty-result behaviour when TenantId is absent.
    /// Logging is exercised via the decorator pipeline – no ILogger is injected.
    /// </summary>
    [TestClass]
    public class GetCategoriesQueryHandlerTest
    {
        private const int TestTenantId = 7;

        private GetCategoriesQueryHandler _sut;
        private Mock<IMindedExampleContext> _contextMock;
        private Mock<ICurrentUserAccessor> _currentUserAccessorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _contextMock = new Mock<IMindedExampleContext>();
            _currentUserAccessorMock = new Mock<ICurrentUserAccessor>();
            _sut = new GetCategoriesQueryHandler(_contextMock.Object, _currentUserAccessorMock.Object);
        }

        /// <summary>
        /// When the current user has no TenantId the handler must return an empty collection
        /// without querying the database.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_ReturnsEmptyList_WhenTenantIdIsNull()
        {
            _currentUserAccessorMock.SetupGet(a => a.TenantId).Returns((int?)null);

            IQueryResponse<IEnumerable<MindedExample.Domain.Category>> response =
                await _sut.HandleAsync(new GetCategoriesQuery());

            response.Should().NotBeNull();
            response.Result.Should().BeEmpty();
            _contextMock.VerifyGet(c => c.Categories, Times.Never);
        }

        /// <summary>
        /// When the current user has a TenantId, the handler must return all categories
        /// that belong to that tenant.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_ReturnsCategories_WhenTenantIdIsSet()
        {
            _currentUserAccessorMock.SetupGet(a => a.TenantId).Returns(TestTenantId);

            var categories = new List<MindedExample.Domain.Category>
            {
                new MindedExample.Domain.Category { Id = Any.Int(), Name = Any.String(), User = new MindedExample.Domain.User { TenantId = TestTenantId } },
                new MindedExample.Domain.Category { Id = Any.Int(), Name = Any.String(), User = new MindedExample.Domain.User { TenantId = TestTenantId } }
            };
            Mock<DbSet<MindedExample.Domain.Category>> mockDbSet = categories.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Categories).Returns(mockDbSet.Object);

            IQueryResponse<IEnumerable<MindedExample.Domain.Category>> response =
                await _sut.HandleAsync(new GetCategoriesQuery());

            response.Should().NotBeNull();
            response.Result.Should().HaveCount(2);
        }

        /// <summary>
        /// The handler must only return categories belonging to the current tenant,
        /// excluding categories from other tenants.
        /// </summary>
        [TestMethod]
        public async Task HandleAsync_ReturnsOnlyCurrentTenantCategories_WhenMultipleTenantsExist()
        {
            const int otherTenantId = 99;
            _currentUserAccessorMock.SetupGet(a => a.TenantId).Returns(TestTenantId);

            var categories = new List<MindedExample.Domain.Category>
            {
                new MindedExample.Domain.Category { Id = 1, Name = Any.String(), User = new MindedExample.Domain.User { TenantId = TestTenantId } },
                new MindedExample.Domain.Category { Id = 2, Name = Any.String(), User = new MindedExample.Domain.User { TenantId = TestTenantId } },
                new MindedExample.Domain.Category { Id = 3, Name = Any.String(), User = new MindedExample.Domain.User { TenantId = otherTenantId } }
            };
            Mock<DbSet<MindedExample.Domain.Category>> mockDbSet = categories.AsQueryable().GetMockDbSet();
            _contextMock.Setup(c => c.Categories).Returns(mockDbSet.Object);

            IQueryResponse<IEnumerable<MindedExample.Domain.Category>> response =
                await _sut.HandleAsync(new GetCategoriesQuery());

            response.Result.Should().HaveCount(2);
            response.Result.Should().OnlyContain(c => c.User.TenantId == TestTenantId);
        }
    }
}
