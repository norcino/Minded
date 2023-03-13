using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using Service.Category.Command;

namespace Service.Category.CommandHandler
{
    public class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand>
    {
        private readonly IMindedExampleContext _context;

        public DeleteCategoryCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<ICommandResponse> HandleAsync(DeleteCategoryCommand command)
        {
            var category = await _context.Categories.SingleOrDefaultAsync(c => c.Id == command.CategoryId);

            if(category == null)
            {
                return new CommandResponse<Data.Entity.Category>()
                {
                    Successful = true
                };
            }

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            return new CommandResponse<Data.Entity.Category>()
            {
                Successful = true
            };
        }
    }
}
