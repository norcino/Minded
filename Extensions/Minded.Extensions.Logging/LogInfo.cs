namespace Minded.Extensions.Logging
{
    public class LogInfo
    {
        public LogInfo()
        {
            LogMessageTemplate = string.Empty;
            LogMessageParameters = new object[0];
        }

        public LogInfo(string logMessageTemplate, params object[] logMessageParameters)
        {
            LogMessageTemplate = logMessageTemplate;
            LogMessageParameters = logMessageParameters;
        }

        public string LogMessageTemplate { get; private set; }

        public object[] LogMessageParameters { get; private set; }
    }
}
