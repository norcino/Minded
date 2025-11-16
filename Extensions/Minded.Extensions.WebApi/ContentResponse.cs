namespace Minded.Extensions.WebApi
{
    public enum ContentResponse : int
    {
        // Does not return any content
        None = 0,
        // Return the specific result type T of ICommand<T>, behave like Full on ICommand and IQuery
        Result = 1,
        // Returns the whole ICommand or IQuery response
        Full = 2
    }
}
