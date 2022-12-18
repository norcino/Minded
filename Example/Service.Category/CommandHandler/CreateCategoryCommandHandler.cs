using System.Threading.Tasks;
using Data.Context;
using Minded.Framework.CQRS.Command;
using Service.Category.Command;

namespace Service.Category.CommandHandler
{
    public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand>
    {
        private readonly IMindedExampleContext _context;

        public CreateCategoryCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        public async Task<ICommandResponse> HandleAsync(CreateCategoryCommand command)
        {
            await _context.Categories.AddAsync(command.Category);
            await _context.SaveChangesAsync();

            return new CommandResponse<int>(command.Category.Id)
            {
                Successful = true
            };
        }
    }
}
