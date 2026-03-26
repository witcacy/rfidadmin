using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Rfid.IngestionService.Models;


namespace Rfid.IngestionService.Distribution.SignalR;

/// <summary>
/// Sends processed RFID events to a remote SignalR hub hosted by Rfid.WebApi.
/// This class is a pure distribution component — no filtering, transformation, or business logic.
/// </summary>
public class SignalRPublisher : IAsyncDisposable
{
    private readonly ILogger<SignalRPublisher> _logger;
    private readonly HubConnection _hubConnection;

    public SignalRPublisher(ILogger<SignalRPublisher> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Read the hub URL from configuration (e.g. "SignalR:HubUrl").
        var hubUrl = configuration["SignalR:HubUrl"]
            ?? throw new InvalidOperationException("Configuration value 'SignalR:HubUrl' is required.");

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect() // Built-in retry with default back-off intervals.
            .Build();

        // Log connection state changes.
        _hubConnection.Reconnecting += error =>
        {
            _logger.LogWarning(error, "SignalR connection lost. Reconnecting...");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.LogInformation("SignalR reconnected with connection ID {ConnectionId}.", connectionId);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += error =>
        {
            _logger.LogWarning(error, "SignalR connection closed.");
            return Task.CompletedTask;
        };

        // TODO: Configure authentication / access tokens for the hub connection.
    }

    /// <summary>
    /// Starts the underlying SignalR hub connection.
    /// Should be called once during application startup.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SignalR hub connection...");
        await _hubConnection.StartAsync(cancellationToken);
        _logger.LogInformation("SignalR hub connection started. State: {State}.", _hubConnection.State);
    }

    /// <summary>
    /// Publishes the event to the configured SignalR hub by invoking a server-side hub method.
    /// </summary>
    public async Task PublishAsync(RfidEvent rfidEvent, CancellationToken cancellationToken)
    {
        if (_hubConnection.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("SignalR hub is not connected (State: {State}). Skipping publish for EPC {Epc}.",
                _hubConnection.State, rfidEvent.Epc);
            return;
        }

        _logger.LogDebug("Publishing event to SignalR for EPC {Epc}.", rfidEvent.Epc);

        // Invoke the hub method on the remote Rfid.WebApi SignalR hub.
        // TODO: Make the hub method name configurable if it changes on the server.
        await _hubConnection.SendAsync("ReceiveRfidEvent", rfidEvent, cancellationToken);
    }

    /// <summary>
    /// Publishes a reader status change to the SignalR hub for dashboard updates.
    /// </summary>
    public async Task PublishReaderStatusAsync(ReaderStatusEvent statusEvent, CancellationToken cancellationToken)
    {
        if (_hubConnection.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("SignalR hub is not connected (State: {State}). Skipping reader status publish for {ReaderId}.",
                _hubConnection.State, statusEvent.ReaderId);
            return;
        }

        _logger.LogDebug("Publishing reader status to SignalR for {ReaderId}.", statusEvent.ReaderId);
        await _hubConnection.SendAsync("ReceiveReaderStatus", statusEvent, cancellationToken);
    }

    /// <summary>
    /// Gracefully stops the hub connection.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping SignalR hub connection...");
        await _hubConnection.StopAsync(cancellationToken);
        _logger.LogInformation("SignalR hub connection stopped.");
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
