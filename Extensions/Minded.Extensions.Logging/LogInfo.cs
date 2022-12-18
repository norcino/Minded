using System;

namespace Minded.Extensions.Logging
{
    /// <summary>
    /// Object containing the template and parameter values
    /// </summary>
    public class LogData
    {
        public Guid TraceId { get; } = new Guid();
        public string LogMessageTemplate { get; private set; }
        public object[] LogMessageParameters { get; private set; }

        public LogData()
        {
            LogMessageTemplate = string.Empty;
            LogMessageParameters = new object[0];
        }

        public LogData(Guid? traceId = null, string logMessageTemplate = "", params object[] logMessageParameters)
        {
            LogMessageTemplate = logMessageTemplate;
            LogMessageParameters = logMessageParameters;
            TraceId = traceId ?? TraceId;
        }
    }
}
