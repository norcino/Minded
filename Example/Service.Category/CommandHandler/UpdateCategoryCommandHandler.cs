using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using Service.Category.Command;

namespace Service.Category.CommandHandler
{
    /// <summary>
    /// Handler for updating categories.
    /// The validator ensures the category exists before this handler is called.
    /// If validation fails (category not found), this handler will not be executed.
    /// </summary>
    public class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand>
    {
        private readonly IMindedExampleContext _context;

        public UpdateCategoryCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Updates the category in the database.
        /// Assumes the category exists (validated by UpdateCategoryCommandValidator).
        /// </summary>
        /// <param name="command">The update command containing the category ID and updated data</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Successful command response with the updated category</returns>
        public async Task<ICommandResponse> HandleAsync(UpdateCategoryCommand command, CancellationToken cancellationToken = default)
        {
            Data.Entity.Category category = await _context.Categories.SingleOrDefaultAsync(c => c.Id == command.CategoryId, cancellationToken);

            // Update category properties
            // Note: category should never be null here due to validation, but defensive programming
            if (category != null)
            {
                category.Description = command.Category.Description;
                category.Active = command.Category.Active;
                category.Name = command.Category.Name;

                await _context.SaveChangesAsync(cancellationToken);
            }

            return new CommandResponse<Data.Entity.Category>(command.Category)
            {
                Successful = true
            };
        }
    }
}
