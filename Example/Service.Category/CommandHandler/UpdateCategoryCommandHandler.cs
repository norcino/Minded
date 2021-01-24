using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Common;
using Service.Category.Command;

namespace Service.Category.CommandHandler
{
    public class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand>
    {
        private readonly IMindedExampleContext _context;

        public UpdateCategoryCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<ICommandResponse> HandleAsync(UpdateCategoryCommand command)
        {
            var category = await _context.Categories.SingleOrDefaultAsync(c => c.Id == command.CategoryId);

            category.Description = command.Category.Description;
            category.Active = command.Category.Active;
            category.Name = command.Category.Name;

            await _context.SaveChangesAsync();

            return new CommandResponse<Data.Entity.Category>(command.Category)
            {
                Successful = true
            };
        }
    }
}