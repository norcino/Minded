using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Application.Api.Hubs;

/// <summary>
/// SignalR hub for streaming application logs to connected clients in real-time.
/// Clients can connect to this hub to receive log messages as they are generated.
/// </summary>
public class LogHub : Hub
{
    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        Console.WriteLine($"Client connected to LogHub: {Context.ConnectionId}");
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        Console.WriteLine($"Client disconnected from LogHub: {Context.ConnectionId}");
    }
}

