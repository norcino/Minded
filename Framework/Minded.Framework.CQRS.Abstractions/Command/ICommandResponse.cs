namespace Minded.Framework.CQRS.Command
{
    /// <summary>
    /// Default command response whit information about the success of the command and optionally a list of <see cref="ValidationEntry"/>.
    /// </summary>
    public interface ICommandResponse
    {
        bool Successful { get; set; }
    }
    
    /// <summary>
    /// Extended version of <see cref="ICommandResponse"/> with a generic type Result, containing possible Command output.
    /// </summary>
    /// <typeparam name="TResult">Command result</typeparam>
    public interface ICommandResponse<out TResult> : ICommandResponse
    {
        TResult Result { get; }
    }
}
