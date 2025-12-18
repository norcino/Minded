using System;
using Serilog;
using Serilog.Configuration;
using Microsoft.AspNetCore.SignalR;
using Application.Api.Hubs;

namespace Application.Api.Logging;

/// <summary>
/// Extension methods for configuring the SignalR sink in Serilog.
/// </summary>
public static class SignalRSinkExtensions
{
    /// <summary>
    /// Adds the SignalR sink to the Serilog logger configuration.
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration.</param>
    /// <param name="hubContext">The SignalR hub context.</param>
    /// <returns>The logger configuration for method chaining.</returns>
    public static LoggerConfiguration SignalR(
        this LoggerSinkConfiguration loggerConfiguration,
        IHubContext<LogHub> hubContext)
    {
        if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
        if (hubContext == null) throw new ArgumentNullException(nameof(hubContext));

        return loggerConfiguration.Sink(new SignalRSink(hubContext));
    }
}

