namespace Minded.Extensions.Retry.Configuration
{
    /// <summary>
    /// Configuration options for retry decorators.
    /// Provides default values for retry count and delays when not specified in attributes.
    /// </summary>
    public class RetryOptions
    {
        /// <summary>
        /// Gets or sets the default number of retry attempts before failing.
        /// Default value is 3 if not configured.
        /// </summary>
        public int DefaultRetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the default delay in milliseconds before the first retry.
        /// If this is the only delay specified, it will be used for all retries.
        /// Default value is 0 (no delay).
        /// </summary>
        public int DefaultDelay1 { get; set; } = 0;

        /// <summary>
        /// Gets or sets the default delay in milliseconds before the second retry.
        /// Default value is 0 (uses DefaultDelay1).
        /// </summary>
        public int DefaultDelay2 { get; set; } = 0;

        /// <summary>
        /// Gets or sets the default delay in milliseconds before the third retry.
        /// Default value is 0 (uses previous delay).
        /// </summary>
        public int DefaultDelay3 { get; set; } = 0;

        /// <summary>
        /// Gets or sets the default delay in milliseconds before the fourth retry.
        /// Default value is 0 (uses previous delay).
        /// </summary>
        public int DefaultDelay4 { get; set; } = 0;

        /// <summary>
        /// Gets or sets the default delay in milliseconds before the fifth retry.
        /// Default value is 0 (uses previous delay).
        /// </summary>
        public int DefaultDelay5 { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to apply retry logic to all queries by default,
        /// even if they don't have the RetryQueryAttribute.
        /// Default value is false (only queries with RetryQueryAttribute are retried).
        /// </summary>
        public bool ApplyToAllQueries { get; set; } = false;

        /// <summary>
        /// Gets the delay for a specific retry iteration using default values.
        /// </summary>
        /// <param name="iteration">The retry iteration number (1-based)</param>
        /// <returns>The delay in milliseconds</returns>
        public int GetDefaultDelayForIteration(int iteration)
        {
            switch (iteration)
            {
                case 1: return DefaultDelay1;
                case 2: return DefaultDelay2 > 0 ? DefaultDelay2 : DefaultDelay1;
                case 3: return DefaultDelay3 > 0 ? DefaultDelay3 : (DefaultDelay2 > 0 ? DefaultDelay2 : DefaultDelay1);
                case 4: return DefaultDelay4 > 0 ? DefaultDelay4 : (DefaultDelay3 > 0 ? DefaultDelay3 : (DefaultDelay2 > 0 ? DefaultDelay2 : DefaultDelay1));
                case 5: return DefaultDelay5 > 0 ? DefaultDelay5 : (DefaultDelay4 > 0 ? DefaultDelay4 : (DefaultDelay3 > 0 ? DefaultDelay3 : (DefaultDelay2 > 0 ? DefaultDelay2 : DefaultDelay1)));
                default:
                    // For iterations beyond 5, use the last configured delay
                    return DefaultDelay5 > 0 ? DefaultDelay5 : (DefaultDelay4 > 0 ? DefaultDelay4 : (DefaultDelay3 > 0 ? DefaultDelay3 : (DefaultDelay2 > 0 ? DefaultDelay2 : DefaultDelay1)));
            }
        }
    }
}

