using System;

namespace Minded.Extensions.Retry.Configuration
{
    /// <summary>
    /// Configuration options for retry decorators.
    /// Provides default values for retry count and delays when not specified in attributes.
    /// All properties support both static values and dynamic providers for runtime configuration (e.g., feature flags).
    /// </summary>
    public class RetryOptions
    {
        /// <summary>
        /// Gets or sets the default number of retry attempts before failing.
        /// This property is used as the default value when DefaultRetryCountProvider is not set.
        /// Default value is 3 if not configured.
        /// </summary>
        public int DefaultRetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets a function that dynamically determines the default retry count.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over DefaultRetryCount.
        /// The function is called each time a retry operation is initiated.
        /// Example: () => _configService.GetValue("retry-count", 3)
        /// Default: null (uses DefaultRetryCount instead)
        /// </summary>
        public Func<int> DefaultRetryCountProvider { get; set; }

        /// <summary>
        /// Gets or sets the default delay in milliseconds before the first retry.
        /// If this is the only delay specified, it will be used for all retries.
        /// This property is used as the default value when DefaultDelay1Provider is not set.
        /// Default value is 0 (no delay).
        /// </summary>
        public int DefaultDelay1 { get; set; } = 0;

        /// <summary>
        /// Gets or sets a function that dynamically determines the default delay before the first retry.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over DefaultDelay1.
        /// The function is called each time a retry operation is initiated.
        /// Example: () => _configService.GetValue("retry-delay1", 0)
        /// Default: null (uses DefaultDelay1 instead)
        /// </summary>
        public Func<int> DefaultDelay1Provider { get; set; }

        /// <summary>
        /// Gets or sets the default delay in milliseconds before the second retry.
        /// This property is used as the default value when DefaultDelay2Provider is not set.
        /// Default value is 0 (uses DefaultDelay1).
        /// </summary>
        public int DefaultDelay2 { get; set; } = 0;

        /// <summary>
        /// Gets or sets a function that dynamically determines the default delay before the second retry.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over DefaultDelay2.
        /// The function is called each time a retry operation is initiated.
        /// Example: () => _configService.GetValue("retry-delay2", 0)
        /// Default: null (uses DefaultDelay2 instead)
        /// </summary>
        public Func<int> DefaultDelay2Provider { get; set; }

        /// <summary>
        /// Gets or sets the default delay in milliseconds before the third retry.
        /// This property is used as the default value when DefaultDelay3Provider is not set.
        /// Default value is 0 (uses previous delay).
        /// </summary>
        public int DefaultDelay3 { get; set; } = 0;

        /// <summary>
        /// Gets or sets a function that dynamically determines the default delay before the third retry.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over DefaultDelay3.
        /// The function is called each time a retry operation is initiated.
        /// Example: () => _configService.GetValue("retry-delay3", 0)
        /// Default: null (uses DefaultDelay3 instead)
        /// </summary>
        public Func<int> DefaultDelay3Provider { get; set; }

        /// <summary>
        /// Gets or sets the default delay in milliseconds before the fourth retry.
        /// This property is used as the default value when DefaultDelay4Provider is not set.
        /// Default value is 0 (uses previous delay).
        /// </summary>
        public int DefaultDelay4 { get; set; } = 0;

        /// <summary>
        /// Gets or sets a function that dynamically determines the default delay before the fourth retry.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over DefaultDelay4.
        /// The function is called each time a retry operation is initiated.
        /// Example: () => _configService.GetValue("retry-delay4", 0)
        /// Default: null (uses DefaultDelay4 instead)
        /// </summary>
        public Func<int> DefaultDelay4Provider { get; set; }

        /// <summary>
        /// Gets or sets the default delay in milliseconds before the fifth retry.
        /// This property is used as the default value when DefaultDelay5Provider is not set.
        /// Default value is 0 (uses previous delay).
        /// </summary>
        public int DefaultDelay5 { get; set; } = 0;

        /// <summary>
        /// Gets or sets a function that dynamically determines the default delay before the fifth retry.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over DefaultDelay5.
        /// The function is called each time a retry operation is initiated.
        /// Example: () => _configService.GetValue("retry-delay5", 0)
        /// Default: null (uses DefaultDelay5 instead)
        /// </summary>
        public Func<int> DefaultDelay5Provider { get; set; }

        /// <summary>
        /// Gets or sets whether to apply retry logic to all queries by default,
        /// even if they don't have the RetryQueryAttribute.
        /// This property is used as the default value when ApplyToAllQueriesProvider is not set.
        /// Default value is false (only queries with RetryQueryAttribute are retried).
        /// </summary>
        public bool ApplyToAllQueries { get; set; } = false;

        /// <summary>
        /// Gets or sets a function that dynamically determines whether to apply retry logic to all queries.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over ApplyToAllQueries.
        /// The function is called each time a query is executed.
        /// Example: () => _featureFlagService.IsEnabled("retry-all-queries")
        /// Default: null (uses ApplyToAllQueries instead)
        /// </summary>
        public Func<bool> ApplyToAllQueriesProvider { get; set; }

        /// <summary>
        /// Gets the effective default retry count.
        /// Uses DefaultRetryCountProvider if set, otherwise falls back to DefaultRetryCount.
        /// This method is called each time a retry operation is initiated.
        /// </summary>
        /// <returns>The effective default retry count.</returns>
        public int GetEffectiveDefaultRetryCount()
        {
            return DefaultRetryCountProvider?.Invoke() ?? DefaultRetryCount;
        }

        /// <summary>
        /// Gets the effective default delay for the first retry.
        /// Uses DefaultDelay1Provider if set, otherwise falls back to DefaultDelay1.
        /// This method is called each time a retry operation is initiated.
        /// </summary>
        /// <returns>The effective default delay in milliseconds.</returns>
        public int GetEffectiveDefaultDelay1()
        {
            return DefaultDelay1Provider?.Invoke() ?? DefaultDelay1;
        }

        /// <summary>
        /// Gets the effective default delay for the second retry.
        /// Uses DefaultDelay2Provider if set, otherwise falls back to DefaultDelay2.
        /// This method is called each time a retry operation is initiated.
        /// </summary>
        /// <returns>The effective default delay in milliseconds.</returns>
        public int GetEffectiveDefaultDelay2()
        {
            return DefaultDelay2Provider?.Invoke() ?? DefaultDelay2;
        }

        /// <summary>
        /// Gets the effective default delay for the third retry.
        /// Uses DefaultDelay3Provider if set, otherwise falls back to DefaultDelay3.
        /// This method is called each time a retry operation is initiated.
        /// </summary>
        /// <returns>The effective default delay in milliseconds.</returns>
        public int GetEffectiveDefaultDelay3()
        {
            return DefaultDelay3Provider?.Invoke() ?? DefaultDelay3;
        }

        /// <summary>
        /// Gets the effective default delay for the fourth retry.
        /// Uses DefaultDelay4Provider if set, otherwise falls back to DefaultDelay4.
        /// This method is called each time a retry operation is initiated.
        /// </summary>
        /// <returns>The effective default delay in milliseconds.</returns>
        public int GetEffectiveDefaultDelay4()
        {
            return DefaultDelay4Provider?.Invoke() ?? DefaultDelay4;
        }

        /// <summary>
        /// Gets the effective default delay for the fifth retry.
        /// Uses DefaultDelay5Provider if set, otherwise falls back to DefaultDelay5.
        /// This method is called each time a retry operation is initiated.
        /// </summary>
        /// <returns>The effective default delay in milliseconds.</returns>
        public int GetEffectiveDefaultDelay5()
        {
            return DefaultDelay5Provider?.Invoke() ?? DefaultDelay5;
        }

        /// <summary>
        /// Gets the effective setting for applying retry logic to all queries.
        /// Uses ApplyToAllQueriesProvider if set, otherwise falls back to ApplyToAllQueries.
        /// This method is called each time a query is executed.
        /// </summary>
        /// <returns>True if retry logic should be applied to all queries, false otherwise.</returns>
        public bool GetEffectiveApplyToAllQueries()
        {
            return ApplyToAllQueriesProvider?.Invoke() ?? ApplyToAllQueries;
        }

        /// <summary>
        /// Gets the delay for a specific retry iteration using effective default values.
        /// </summary>
        /// <param name="iteration">The retry iteration number (1-based)</param>
        /// <returns>The delay in milliseconds</returns>
        public int GetDefaultDelayForIteration(int iteration)
        {
            var delay1 = GetEffectiveDefaultDelay1();
            var delay2 = GetEffectiveDefaultDelay2();
            var delay3 = GetEffectiveDefaultDelay3();
            var delay4 = GetEffectiveDefaultDelay4();
            var delay5 = GetEffectiveDefaultDelay5();

            switch (iteration)
            {
                case 1: return delay1;
                case 2: return delay2 > 0 ? delay2 : delay1;
                case 3: return delay3 > 0 ? delay3 : (delay2 > 0 ? delay2 : delay1);
                case 4: return delay4 > 0 ? delay4 : (delay3 > 0 ? delay3 : (delay2 > 0 ? delay2 : delay1));
                case 5: return delay5 > 0 ? delay5 : (delay4 > 0 ? delay4 : (delay3 > 0 ? delay3 : (delay2 > 0 ? delay2 : delay1)));
                default:
                    // For iterations beyond 5, use the last configured delay
                    return delay5 > 0 ? delay5 : (delay4 > 0 ? delay4 : (delay3 > 0 ? delay3 : (delay2 > 0 ? delay2 : delay1)));
            }
        }
    }
}

