using Microsoft.Extensions.Logging;

namespace Minded.Extensions.Logging
{
    public static class LogEvent
    {
        static LogEvent()
        {
            QueryHandling = new EventId(1, "QueryHandling");
            CommandHandling = new EventId(2, "CommandHandling");
        }

        public static EventId QueryHandling { get; }
        public static EventId CommandHandling { get; }
    }
}
