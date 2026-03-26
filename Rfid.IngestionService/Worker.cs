using Rfid.IngestionService.EventEngine;
using Rfid.IngestionService.Llrp;

namespace Rfid.IngestionService;

/// <summary>
/// Background service that orchestrates the RFID ingestion pipeline.
/// <para>
/// Responsibilities are limited to lifecycle management:
/// <list type="bullet">
///   <item>Start all LLRP reader connections on service start.</item>
///   <item>Run a periodic health-check / purge loop while the host is running.</item>
///   <item>Stop all reader connections gracefully on shutdown.</item>
/// </list>
/// All reader management is delegated to <see cref="LlrpConnectionManager"/>.
/// This class does NOT contain RFID processing or business logic.
/// </para>
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly LlrpConnectionManager _connectionManager;
    private readonly DeduplicationService _deduplicationService;

    /// <summary>Interval between health-check / maintenance cycles.</summary>
    private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromSeconds(30);

    public Worker(
        ILogger<Worker> logger,
        LlrpConnectionManager connectionManager,
        DeduplicationService deduplicationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _deduplicationService = deduplicationService ?? throw new ArgumentNullException(nameof(deduplicationService));
    }

    /// <summary>
    /// Entry point invoked by the .NET Generic Host. Starts the LLRP connection
    /// layer and keeps the service alive until cancellation is requested.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RFID Ingestion Service starting.");

        try
        {
            await _connectionManager.StartAllAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(ex, "Failed to start reader connections. The service will shut down.");
            return;
        }

        _logger.LogInformation("RFID Ingestion Service is running.");

        // Periodic health-check loop: purge stale deduplication entries and
        // log reader connection states.
        try
        {
            using var timer = new PeriodicTimer(HealthCheckInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var purged = _deduplicationService.PurgeStaleEntries();
                if (purged > 0)
                    _logger.LogDebug("Deduplication maintenance: purged {Count} stale entries.", purged);

                var states = _connectionManager.GetReaderStates();
                foreach (var (readerId, state) in states)
                {
                    _logger.LogDebug("Reader {ReaderId} state: {State}.", readerId, state);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected -- the host requested a graceful shutdown.
        }
    }

    /// <summary>
    /// Called by the host when the service is stopping. Gracefully tears down
    /// all reader connections before the host terminates.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RFID Ingestion Service stopping.");

        try
        {
            await _connectionManager.StopAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while stopping reader connections.");
        }

        _logger.LogInformation("RFID Ingestion Service stopped.");

        await base.StopAsync(cancellationToken);
    }
}
