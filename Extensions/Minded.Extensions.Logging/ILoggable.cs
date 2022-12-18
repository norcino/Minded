namespace Minded.Extensions.Logging
{
    /// <summary>
    /// Describes a log enabled object which can be converted to a template a list of parameters
    /// </summary>
    public interface ILoggable
    {
        LogData ToLog();
    }
}
