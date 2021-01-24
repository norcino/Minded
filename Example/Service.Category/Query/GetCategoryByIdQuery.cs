using Minded.CommandQuery.Query;
using Minded.Log;

namespace Service.Category.Query
{
    public class GetCategoryByIdQuery : IQuery<Data.Entity.Category>
    {
        public int CategoryId { get; }

        public GetCategoryByIdQuery(int categoryId)
        {
            CategoryId = categoryId;
        }
        
        public LogInfo ToLog()
        {
            const string template = "CategoryId: {CategoryId}";
            return new LogInfo(template, CategoryId);
        }
    }
}
