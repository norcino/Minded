namespace Minded.Extensions.Logging
{
    /// <summary>
    /// Describes a log enabled object which can be converted to a template a list of parameters
    /// </summary>
    public interface ILoggable
    {
        /// <summary>
        /// Template to be used for string interpolation
        /// </summary>
        string LoggingTemplate { get; }

        /// <summary>
        /// List of parameters which will be substituted in the template
        /// </summary>
        object[] LoggingParameters { get; }
    }
}
