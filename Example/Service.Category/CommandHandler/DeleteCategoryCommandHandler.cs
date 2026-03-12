using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using Service.Category.Command;

namespace Service.Category.CommandHandler
{
    /// <summary>
    /// Handler for deleting categories.
    /// The validator ensures the category exists before this handler is called.
    /// If validation fails, this handler will not be executed.
    /// </summary>
    public class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand>
    {
        private readonly IMindedExampleContext _context;

        public DeleteCategoryCommandHandler(IMindedExampleContext context)
        {
            _context = context;
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
            Data.Entity.Category category = await _context.Categories.SingleOrDefaultAsync(c => c.Id == command.CategoryId, cancellationToken);

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);

            return new CommandResponse
            {
                Successful = true
            };
        }
    }
}
