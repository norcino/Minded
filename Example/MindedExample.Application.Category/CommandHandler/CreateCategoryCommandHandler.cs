using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Category.Command;

namespace MindedExample.Application.Category.CommandHandler
{
    /// <summary>
    /// Handler for creating new categories.
    /// All validation (including tenant and user membership checks) is performed by
    /// <see cref="MindedExample.Application.Category.Validator.CreateCategoryCommandValidator"/>
    /// via the <c>[ValidateCommand]</c> decorator before this handler is invoked.
    /// </summary>
    public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, MindedExample.Domain.Category>
    {
        private readonly IMindedExampleContext _context;

        public CreateCategoryCommandHandler(IMindedExampleContext context) => _context = context;

        /// <summary>
        /// Persists the new category to the database.
        /// </summary>
        /// <param name="command">The create command containing the category to persist.</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
        /// <returns>A successful command response containing the created category.</returns>
        public async Task<ICommandResponse<MindedExample.Domain.Category>> HandleAsync(
            CreateCategoryCommand command, CancellationToken cancellationToken = default)
        {
            await _context.Categories.AddAsync(command.Category, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return CommandResponse<MindedExample.Domain.Category>.Success(command.Category);
        }
    }
}
