namespace Minded.Framework.CQRS.Command
{
    /// <summary>
    /// Base command interface
    /// </summary>
    public interface ICommand
    {        
    }

    /// <summary>
    /// Command interface that returns a result object
    /// </summary>
    /// <typeparam name="TResult">Result type</typeparam>
    public interface ICommand<TResult> : ICommand
    {
        TResult Result { get; }
    }
}
