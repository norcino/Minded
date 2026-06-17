using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.Mediator;
using MindedExample.Application.Category.Command;
using MindedExample.Application.Category.Validator;
using MindedExample.Application.User.Query;
using Moq;

namespace MindedExample.Application.Category.UnitTests
{
    [TestClass]
    public class CreateCategoryCommandValidatorTest
    {
        private const int CurrentUserId = 42;

        private CreateCategoryCommandValidator _sut;
        private Mock<IMediator> _mediatorMock;

        [TestInitialize]
        public void TestInitialize()
        {
            _mediatorMock = new Mock<IMediator>();
            _mediatorMock
                .Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsUserInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _sut = new CreateCategoryCommandValidator(new CategoryValidator(), _mediatorMock.Object);
        }

        [TestMethod]
        public async Task Validation_succeed_when_category_is_valid_for_creation()
        {
            MindedExample.Domain.Category category = Builder<MindedExample.Domain.Category>.New()
                .Build(e => { e.Name = "Category name"; e.Id = 0; e.UserId = CurrentUserId; e.ParentId = 1; });
            var command = new CreateCategoryCommand(category);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.OutcomeEntries.Any(e => e.Severity == Severity.Error));
        }

        [TestMethod]
        public async Task Validation_fails_when_category_in_command_is_null()
        {
            var command = new CreateCategoryCommand(null);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.Category) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} is mandatory"));
        }

        [TestMethod]
        public async Task Validation_fails_when_category_in_command_has_id()
        {
            MindedExample.Domain.Category category = Builder<MindedExample.Domain.Category>.New()
                .Build(e =>
                {
                    e.Name = "Category name";
                    e.Id = 1;
                    e.UserId = CurrentUserId;
                    e.ParentId = 2;
                });

            var command = new CreateCategoryCommand(category);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.Category.Id) &&
                e.Severity == Severity.Error &&
                e.Message == "{0} should not be specified on creation"));
        }

        [TestMethod]
        public async Task Validation_fails_when_user_does_not_exist_in_current_tenant()
        {
            _mediatorMock
                .Setup(m => m.ProcessQueryAsync(It.IsAny<ExistsUserInCurrentTenantQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            MindedExample.Domain.Category category = Builder<MindedExample.Domain.Category>.New()
                .Build(e => { e.Name = "Category name"; e.Id = 0; e.UserId = CurrentUserId; e.ParentId = 1; });

            var command = new CreateCategoryCommand(category);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.OutcomeEntries.Any(e =>
                e.PropertyName == nameof(command.Category.UserId) &&
                e.Severity == Severity.Error));
        }

        [TestMethod]
        public async Task Validation_does_not_check_user_existence_when_userId_is_zero()
        {
            // When UserId == 0 the tenant-existence check is skipped entirely.
            MindedExample.Domain.Category category = Builder<MindedExample.Domain.Category>.New()
                .Build(e => { e.Name = "Category name"; e.Id = 0; e.UserId = 0; e.ParentId = 1; });

            var command = new CreateCategoryCommand(category);
            IValidationResult result = await _sut.ValidateAsync(command);

            Assert.IsNotNull(result);
            _mediatorMock.Verify(
                m => m.ProcessQueryAsync(It.IsAny<ExistsUserInCurrentTenantQuery>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Validation_succeeds_when_authContextAccessor_is_null()
        {
            // When no IAuthorizationContextAccessor is injected, root-category permission check is skipped.
            var sut = new CreateCategoryCommandValidator(new CategoryValidator(), _mediatorMock.Object, authContextAccessor: null);

            MindedExample.Domain.Category category = Builder<MindedExample.Domain.Category>.New()
                .Build(e => { e.Name = "Category name"; e.Id = 0; e.UserId = 0; e.ParentId = 1; });

            var command = new CreateCategoryCommand(category);
            IValidationResult result = await sut.ValidateAsync(command);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
        }
    }
}
