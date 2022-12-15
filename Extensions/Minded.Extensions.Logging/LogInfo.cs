using System;

namespace Minded.Extensions.Logging
{
    public class LogInfo
    {
        public LogInfo()
        {
            TrackingId = Guid.NewGuid();
            LogMessageTemplate = string.Empty;
            LogMessageParameters = new object[0];
        }

        public LogInfo(Guid trackingId, string logMessageTemplate = "", params object[] logMessageParameters)
        {
            TrackingId = trackingId;
            LogMessageTemplate = logMessageTemplate;
            LogMessageParameters = logMessageParameters;
        }

        public Guid TrackingId { get; private set; }
        public string LogMessageTemplate { get; private set; }
        public object[] LogMessageParameters { get; private set; }
    }
}
