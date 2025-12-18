using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Serilog.Core;
using Serilog.Events;
using Application.Api.Hubs;

namespace Application.Api.Logging;

/// <summary>
/// Custom Serilog sink that sends log events to connected SignalR clients in real-time.
/// Implements a background queue to avoid blocking the logging pipeline.
/// </summary>
public class SignalRSink : ILogEventSink, IDisposable
{
    private readonly IHubContext<LogHub> _hubContext;
    private readonly BlockingCollection<LogEvent> _logQueue;
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the SignalRSink.
    /// </summary>
    /// <param name="hubContext">The SignalR hub context for broadcasting log messages.</param>
    public SignalRSink(IHubContext<LogHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logQueue = new BlockingCollection<LogEvent>(new ConcurrentQueue<LogEvent>());
        _cancellationTokenSource = new CancellationTokenSource();
        _processingTask = Task.Run(ProcessLogQueue);
    }

    /// <summary>
    /// Emits a log event to the sink. Adds the event to a queue for background processing.
    /// </summary>
    /// <param name="logEvent">The log event to emit.</param>
    public void Emit(LogEvent logEvent)
    {
        if (logEvent == null) return;

        // Add to queue without blocking
        if (!_logQueue.TryAdd(logEvent, 0))
        {
            // Queue is full, drop the log event
            Console.WriteLine("SignalRSink: Log queue is full, dropping log event");
        }
    }

    /// <summary>
    /// Background task that processes queued log events and sends them to SignalR clients.
    /// </summary>
    private async Task ProcessLogQueue()
    {
        try
        {
            foreach (var logEvent in _logQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                try
                {
                    var logMessage = new
                    {
                        Timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        Level = logEvent.Level.ToString(),
                        Message = logEvent.RenderMessage(),
                        Exception = logEvent.Exception?.ToString(),
                        Properties = logEvent.Properties.ToDictionary(
                            p => p.Key,
                            p => p.Value.ToString()
                        )
                    };

                    // Broadcast to all connected clients
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", logMessage, _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SignalRSink: Error sending log to SignalR: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when shutting down
        }
    }

    /// <summary>
    /// Disposes the sink and stops the background processing task.
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _logQueue.CompleteAdding();
        
        try
        {
            _processingTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalRSink: Error waiting for processing task: {ex.Message}");
        }

        _logQueue.Dispose();
        _cancellationTokenSource.Dispose();
    }
}

