using Minded.Extensions.Logging;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace Service.Category.Command
{
    [ValidateCommand]
    public class CreateCategoryCommand : ICommand
    {
        public Data.Entity.Category Category { get; set; }

        public CreateCategoryCommand(Data.Entity.Category category)
        {
            Category = category;
        }

        public LogInfo ToLog()
        {
            const string template = "Category: {Name}";
            return new LogInfo(template, Category.Name);
        }
    }
}
