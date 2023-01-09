using System.Threading.Tasks;
using Data.Context;
using Minded.Framework.CQRS.Command;
using Service.Category.Command;

namespace Service.Category.CommandHandler
{
    public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, Data.Entity.Category>
    {
        private readonly IMindedExampleContext _context;

        public CreateCategoryCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<ICommandResponse<Data.Entity.Category>> HandleAsync(CreateCategoryCommand command)
        {
            await _context.Categories.AddAsync(command.Category);
            await _context.SaveChangesAsync();

            return new CommandResponse<Data.Entity.Category>(command.Category)
            {
                Successful = true
            };
        }
    }
}
