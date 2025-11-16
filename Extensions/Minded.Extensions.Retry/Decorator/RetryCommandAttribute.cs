using System;

namespace Minded.Extensions.Retry.Decorator
{
    /// <summary>
    /// Attribute used by RetryCommandHandlerDecorator to determine if a command requires retry logic.
    /// Allows configuration of retry count and delay intervals between retries.
    /// </summary>
    /// <remarks>
    /// Retry delay behavior:
    /// - If no delay values are provided, retries happen immediately
    /// - If only one delay value is provided, it's used for all retries
    /// - If multiple delay values are provided, each controls the delay before the corresponding retry iteration
    /// - If fewer delay values than retry count, the last delay value is used for remaining retries
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RetryCommandAttribute : Attribute
    {
        /// <summary>
        /// Gets the number of retry attempts before failing.
        /// If not set, uses the default from dependency injection configuration, or 3 if not configured.
        /// </summary>
        public int? RetryCount { get; }

        /// <summary>
        /// Gets the delay in milliseconds before the first retry.
        /// If this is the only delay specified, it will be used for all retries.
        /// </summary>
        public int? Delay1 { get; }

        /// <summary>
        /// Gets the delay in milliseconds before the second retry.
        /// </summary>
        public int? Delay2 { get; }

        /// <summary>
        /// Gets the delay in milliseconds before the third retry.
        /// </summary>
        public int? Delay3 { get; }

        /// <summary>
        /// Gets the delay in milliseconds before the fourth retry.
        /// </summary>
        public int? Delay4 { get; }

        /// <summary>
        /// Gets the delay in milliseconds before the fifth retry.
        /// </summary>
        public int? Delay5 { get; }

        /// <summary>
        /// Initializes a new instance of the RetryCommandAttribute with default settings.
        /// </summary>
        public RetryCommandAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the RetryCommandAttribute with specified retry count.
        /// </summary>
        /// <param name="retryCount">Number of retry attempts before failing</param>
        public RetryCommandAttribute(int retryCount)
        {
            RetryCount = retryCount;
        }

        /// <summary>
        /// Initializes a new instance of the RetryCommandAttribute with retry count and delay.
        /// </summary>
        /// <param name="retryCount">Number of retry attempts before failing</param>
        /// <param name="delay1">Delay in milliseconds before retries (used for all retries if no other delays specified)</param>
        public RetryCommandAttribute(int retryCount, int delay1)
        {
            RetryCount = retryCount;
            Delay1 = delay1;
        }

        /// <summary>
        /// Initializes a new instance of the RetryCommandAttribute with retry count and multiple delays.
        /// </summary>
        /// <param name="retryCount">Number of retry attempts before failing</param>
        /// <param name="delay1">Delay in milliseconds before first retry</param>
        /// <param name="delay2">Delay in milliseconds before second retry</param>
        /// <param name="delay3">Delay in milliseconds before third retry (optional)</param>
        /// <param name="delay4">Delay in milliseconds before fourth retry (optional)</param>
        /// <param name="delay5">Delay in milliseconds before fifth retry (optional)</param>
        public RetryCommandAttribute(int retryCount, int delay1, int delay2, int delay3 = 0, int delay4 = 0, int delay5 = 0)
        {
            RetryCount = retryCount;
            Delay1 = delay1;
            Delay2 = delay2;
            if (delay3 > 0) Delay3 = delay3;
            if (delay4 > 0) Delay4 = delay4;
            if (delay5 > 0) Delay5 = delay5;
        }

        /// <summary>
        /// Gets the delay for a specific retry iteration.
        /// </summary>
        /// <param name="iteration">The retry iteration number (1-based)</param>
        /// <returns>The delay in milliseconds, or 0 if no delay is configured</returns>
        public int GetDelayForIteration(int iteration)
        {
            switch (iteration)
            {
                case 1: return Delay1 ?? 0;
                case 2: return Delay2 ?? Delay1 ?? 0;
                case 3: return Delay3 ?? Delay2 ?? Delay1 ?? 0;
                case 4: return Delay4 ?? Delay3 ?? Delay2 ?? Delay1 ?? 0;
                case 5: return Delay5 ?? Delay4 ?? Delay3 ?? Delay2 ?? Delay1 ?? 0;
                default:
                    // For iterations beyond 5, use the last configured delay
                    return Delay5 ?? Delay4 ?? Delay3 ?? Delay2 ?? Delay1 ?? 0;
            }
        }
    }
}

