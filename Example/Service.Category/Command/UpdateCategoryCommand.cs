using Minded.Extensions.Logging;
using Minded.Framework.CQRS.Command;

namespace Service.Category.Command
{
    public class UpdateCategoryCommand : ICommand
    {
        public Data.Entity.Category Category { get; }
        public int CategoryId { get; }

        public UpdateCategoryCommand(int id, Data.Entity.Category category)
        {
            CategoryId = id;
            Category = category;
        }

        //public LogInfo ToLog()
        //{
        //    const string template = "CategoryId: {Id} Category: {Name}";
        //    return new LogInfo(template, Category.Id, Category.Name);
        //}
    }
}
