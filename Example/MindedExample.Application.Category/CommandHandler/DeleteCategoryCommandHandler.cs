using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Category.Command;

namespace MindedExample.Application.Category.CommandHandler
{
    /// <summary>
    /// Handler for deleting categories.
    /// The validator ensures the category exists before this handler is called.
    /// If validation fails, this handler will not be executed.
    /// </summary>
    public class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public DeleteCategoryCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Deletes the category from the database.
        /// Assumes the category exists (validated by DeleteCategoryCommandValidator).
        /// </summary>
        /// <param name="command">The delete command containing the category ID</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Successful command response</returns>
        public async Task<ICommandResponse> HandleAsync(DeleteCategoryCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new CommandResponse { Successful = false };
            }

            MindedExample.Domain.Category category = await _context.Categories
                .SingleOrDefaultAsync(c => c.Id == command.CategoryId && c.User.TenantId == _currentUserAccessor.TenantId.Value, cancellationToken);

            if (category == null)
            {
                return new CommandResponse { Successful = false };
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);

            return new CommandResponse
            {
                Successful = true
            };
        }
    }
}
