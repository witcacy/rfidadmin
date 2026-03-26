using Microsoft.Extensions.Configuration;
using Rfid.IngestionService.Distribution.Mqtt;
using Rfid.IngestionService.Distribution.SignalR;
using Rfid.IngestionService.EventEngine;
using Rfid.IngestionService.Models;
using Rfid.IngestionService.Normalization;

namespace Rfid.IngestionService.Llrp;

/// <summary>
/// Configuration for a single RFID reader endpoint.
/// Bound from the "Readers" section in appsettings.json.
/// </summary>
public sealed class ReaderConfig
{
    /// <summary>Logical identifier for the reader (e.g. "Dock-Door-1").</summary>
    public string ReaderId { get; set; } = string.Empty;

    /// <summary>IP address or hostname of the reader.</summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>TCP port; defaults to the standard LLRP port 5084.</summary>
    public int Port { get; set; } = LlrpClient.DefaultPort;
}

/// <summary>
/// Manages the lifecycle of multiple <see cref="LlrpClient"/> instances,
/// one per configured RFID reader. Responsible for starting all connections
/// when the service starts and stopping them gracefully on shutdown.
/// This class does NOT parse LLRP messages or contain business logic.
/// </summary>
public class LlrpConnectionManager
{
    private readonly ILogger<LlrpConnectionManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<ReaderConfig> _readerConfigs;
    private readonly SignalRPublisher _signalRPublisher;
    private readonly MqttPublisher _mqttPublisher;
    private readonly RfidEventMapper _eventMapper;
    private readonly EventProcessor _eventProcessor;

    /// <summary>Active clients keyed by ReaderId.</summary>
    private readonly Dictionary<string, LlrpClient> _clients = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Connection state per ReaderId, updated via <see cref="LlrpClient.StateChanged"/>.</summary>
    private readonly Dictionary<string, ReaderState> _states = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Synchronises access to <see cref="_states"/>.</summary>
    private readonly object _stateLock = new();

    /// <summary>Tracks active reconnect timers keyed by ReaderId so they can be cancelled on shutdown.</summary>
    private readonly Dictionary<string, CancellationTokenSource> _reconnectTimers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maximum reconnect delay cap.</summary>
    private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromSeconds(60);

    /// <summary>Tracks the current reconnect attempt number per reader for exponential backoff.</summary>
    private readonly Dictionary<string, int> _reconnectAttempts = new(StringComparer.OrdinalIgnoreCase);

    public LlrpConnectionManager(
        ILogger<LlrpConnectionManager> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _signalRPublisher = serviceProvider.GetRequiredService<SignalRPublisher>();
        _mqttPublisher = serviceProvider.GetRequiredService<MqttPublisher>();
        _eventMapper = serviceProvider.GetRequiredService<RfidEventMapper>();
        _eventProcessor = serviceProvider.GetRequiredService<EventProcessor>();

        // Bind the "Llrp:Readers" configuration section into a list of ReaderConfig.
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        _readerConfigs = configuration.GetSection("Llrp:Readers").Get<List<ReaderConfig>>() ?? [];
    }

    /// <summary>
    /// Creates one <see cref="LlrpClient"/> per configured reader and starts them all.
    /// Connections that fail to start are logged but do not prevent other readers from starting.
    /// </summary>
    public async Task StartAllAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting all reader connections ({Count} configured).", _readerConfigs.Count);

        foreach (var config in _readerConfigs)
        {
            if (string.IsNullOrWhiteSpace(config.ReaderId) || string.IsNullOrWhiteSpace(config.Ip))
            {
                _logger.LogWarning("Skipping reader with missing ReaderId or IP.");
                continue;
            }

            await StartReaderAsync(config, cancellationToken);
        }

        _logger.LogInformation("Finished starting reader connections. Active: {Active}/{Total}.",
            _clients.Count, _readerConfigs.Count);
    }

    /// <summary>
    /// Gracefully stops and disposes all active <see cref="LlrpClient"/> instances.
    /// </summary>
    public async Task StopAllAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping all reader connections ({Count} active).", _clients.Count);

        // Cancel all pending reconnect timers first.
        foreach (var (readerId, cts) in _reconnectTimers)
        {
            _logger.LogDebug("[{ReaderId}] Cancelling pending reconnect timer.", readerId);
            await cts.CancelAsync();
            cts.Dispose();
        }
        _reconnectTimers.Clear();
        _reconnectAttempts.Clear();

        foreach (var (readerId, client) in _clients)
        {
            await StopReaderAsync(readerId, client);
        }

        _clients.Clear();
        _states.Clear();

        _logger.LogInformation("All reader connections stopped.");
    }

    /// <summary>
    /// Creates, registers, and starts a single <see cref="LlrpClient"/> for the given configuration.
    /// </summary>
    private async Task StartReaderAsync(ReaderConfig config, CancellationToken cancellationToken)
    {
        var readerId = config.ReaderId;
        _logger.LogInformation("[{ReaderId}] Starting connection to {Ip}:{Port}...",
            readerId, config.Ip, config.Port);

        var clientLogger = _loggerFactory.CreateLogger<LlrpClient>();
        var client = new LlrpClient(readerId, config.Ip, config.Port, clientLogger);

        // Subscribe to state change notifications from the client.
        client.StateChanged += OnReaderStateChanged;

        // Forward raw data events to the normalization pipeline.
        client.DataReceived += OnReaderDataReceived;

        try
        {
            await client.StartAsync(cancellationToken);
            _clients[readerId] = client;
            _reconnectAttempts.Remove(readerId);
            _logger.LogInformation("[{ReaderId}] Connection started successfully.", readerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ReaderId}] Failed to start connection.", readerId);
            client.StateChanged -= OnReaderStateChanged;
            client.DataReceived -= OnReaderDataReceived;
            client.Dispose();

            ScheduleReconnect(config);
        }
    }

    /// <summary>
    /// Stops and disposes a single reader client, logging any errors that occur during shutdown.
    /// </summary>
    private async Task StopReaderAsync(string readerId, LlrpClient client)
    {
        // Cancel any pending reconnect for this reader.
        if (_reconnectTimers.Remove(readerId, out var cts))
        {
            await cts.CancelAsync();
            cts.Dispose();
        }
        _reconnectAttempts.Remove(readerId);

        try
        {
            _logger.LogInformation("[{ReaderId}] Stopping connection...", readerId);
            await client.StopAsync();
            _logger.LogInformation("[{ReaderId}] Connection stopped.", readerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ReaderId}] Error while stopping connection.", readerId);
        }
        finally
        {
            client.StateChanged -= OnReaderStateChanged;
            client.DataReceived -= OnReaderDataReceived;
            client.Dispose();
        }
    }

    // --------------------------------------------------------------
    // State tracking (driven by LlrpClient.StateChanged)
    // --------------------------------------------------------------

    /// <summary>
    /// Callback invoked by each <see cref="LlrpClient"/> when its connection
    /// state changes. Updates the aggregated state dictionary and logs the
    /// transition.
    /// </summary>
    private void OnReaderStateChanged(string readerId, ReaderState previousState, ReaderState newState)
    {
        lock (_stateLock)
        {
            _states[readerId] = newState;
        }

        _logger.LogInformation(
            "[{ReaderId}] Reader state changed: {Previous} -> {New}.",
            readerId, previousState, newState);

        // Publish reader status change to SignalR and MQTT (fire-and-forget, errors logged).
        var statusEvent = new ReaderStatusEvent
        {
            ReaderId = readerId,
            PreviousState = previousState.ToString(),
            NewState = newState.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };

        _ = PublishReaderStatusSafeAsync(statusEvent);

        if (newState == ReaderState.Error)
        {
            _logger.LogWarning(
                "[{ReaderId}] Reader entered Error state. Scheduling reconnect.",
                readerId);

            // Dispose the faulted client and schedule reconnect.
            if (_clients.Remove(readerId, out var faultedClient))
            {
                faultedClient.StateChanged -= OnReaderStateChanged;
                faultedClient.DataReceived -= OnReaderDataReceived;
                faultedClient.Dispose();
            }

            var config = _readerConfigs.Find(c =>
                string.Equals(c.ReaderId, readerId, StringComparison.OrdinalIgnoreCase));

            if (config is not null)
            {
                ScheduleReconnect(config);
            }
        }

        // Reset reconnect attempts on successful connection.
        if (newState == ReaderState.Connected)
        {
            _reconnectAttempts.Remove(readerId);
        }
    }

    /// <summary>
    /// Callback invoked by each <see cref="LlrpClient"/> when raw data is
    /// received from the reader. Logs the receipt for diagnostics.
    /// </summary>
    private void OnReaderDataReceived(string readerId, byte[] data)
    {
        _logger.LogDebug(
            "[{ReaderId}] Raw data received ({Bytes} bytes) via DataReceived event.",
            readerId, data.Length);

        // Forward raw data through the normalization pipeline into the event processor.
        var rfidEvent = _eventMapper.Map(readerId, data);
        if (rfidEvent is null)
            return;

        // Fire-and-forget into the pipeline; errors are logged inside ProcessAsync.
        _ = _eventProcessor.ProcessAsync(rfidEvent, CancellationToken.None);
    }

    // --------------------------------------------------------------
    // Reconnect strategy (exponential backoff)
    // --------------------------------------------------------------

    /// <summary>
    /// Schedules a reconnect attempt for the given reader config using
    /// exponential backoff (2s, 4s, 8s, ... capped at 60s).
    /// </summary>
    private void ScheduleReconnect(ReaderConfig config)
    {
        var readerId = config.ReaderId;

        // Cancel any existing reconnect timer for this reader.
        if (_reconnectTimers.Remove(readerId, out var existingCts))
        {
            existingCts.Cancel();
            existingCts.Dispose();
        }

        _reconnectAttempts.TryGetValue(readerId, out var attempt);
        _reconnectAttempts[readerId] = attempt + 1;

        var delaySeconds = Math.Min(Math.Pow(2, attempt + 1), MaxReconnectDelay.TotalSeconds);
        var delay = TimeSpan.FromSeconds(delaySeconds);

        _logger.LogInformation(
            "[{ReaderId}] Scheduling reconnect attempt {Attempt} in {Delay}s.",
            readerId, attempt + 1, delay.TotalSeconds);

        var cts = new CancellationTokenSource();
        _reconnectTimers[readerId] = cts;

        _ = ReconnectAsync(config, delay, cts.Token);
    }

    /// <summary>
    /// Waits for the specified delay then attempts to reconnect the reader.
    /// </summary>
    private async Task ReconnectAsync(ReaderConfig config, TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("[{ReaderId}] Reconnect timer cancelled.", config.ReaderId);
            return;
        }

        _reconnectTimers.Remove(config.ReaderId);

        _logger.LogInformation("[{ReaderId}] Attempting reconnect...", config.ReaderId);
        await StartReaderAsync(config, cancellationToken);
    }

    // --------------------------------------------------------------
    // Status publishing helpers
    // --------------------------------------------------------------

    /// <summary>
    /// Publishes a reader status event to both SignalR and MQTT.
    /// Failures are logged but do not propagate.
    /// </summary>
    private async Task PublishReaderStatusSafeAsync(ReaderStatusEvent statusEvent)
    {
        try
        {
            await _signalRPublisher.PublishReaderStatusAsync(statusEvent, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{ReaderId}] Failed to publish reader status to SignalR.", statusEvent.ReaderId);
        }

        try
        {
            await _mqttPublisher.PublishReaderStatusAsync(statusEvent, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{ReaderId}] Failed to publish reader status to MQTT.", statusEvent.ReaderId);
        }
    }

    /// <summary>
    /// Returns a point-in-time snapshot of every tracked reader and its
    /// current connection state. Safe to call from any thread.
    /// </summary>
    public IReadOnlyDictionary<string, ReaderState> GetReaderStates()
    {
        lock (_stateLock)
        {
            return new Dictionary<string, ReaderState>(_states, StringComparer.OrdinalIgnoreCase);
        }
    }
}
