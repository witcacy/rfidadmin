using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Rfid.IngestionService.Models;

namespace Rfid.IngestionService.EventEngine;

/// <summary>
/// Prevents duplicate tag events from propagating through the pipeline.
/// <para>
/// Maintains an in-memory cache of recently seen EPCs. An event is
/// considered a duplicate if the same EPC was observed within the
/// configured suppression window. The window is read from
/// <c>Deduplication:WindowSeconds</c> in <c>appsettings.json</c>
/// and defaults to <see cref="DefaultWindowSeconds"/> seconds.
/// </para>
/// </summary>
public class DeduplicationService
{
    /// <summary>Default suppression window when no configuration is provided.</summary>
    internal const int DefaultWindowSeconds = 3;

    private readonly ILogger<DeduplicationService> _logger;
    private readonly TimeSpan _window;

    /// <summary>
    /// In-memory cache: EPC ? last-seen UTC timestamp.
    /// Thread-safe because events may arrive from multiple reader threads.
    /// </summary>
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSeen = new(StringComparer.OrdinalIgnoreCase);

    public DeduplicationService(ILogger<DeduplicationService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var windowMs = configuration?.GetValue<int?>("EventProcessing:DeduplicationWindowMs");
        var seconds = windowMs.HasValue ? windowMs.Value / 1000.0 : DefaultWindowSeconds;
        _window = TimeSpan.FromSeconds(seconds);

        _logger.LogInformation("Deduplication window set to {Window} seconds.", _window.TotalSeconds);
    }

    /// <summary>
    /// Returns <c>true</c> if the event is a duplicate and should be suppressed.
    /// An event is duplicate when the same EPC was last seen within the
    /// configured time window.
    /// </summary>
    public bool IsDuplicate(RfidEvent rfidEvent)
    {
        var now = rfidEvent.Timestamp;
        var epc = rfidEvent.Epc;

        if (_lastSeen.TryGetValue(epc, out var lastTimestamp))
        {
            if (now - lastTimestamp < _window)
            {
                _logger.LogDebug("Duplicate suppressed for EPC {Epc} (last seen {Elapsed} ago).",
                    epc, now - lastTimestamp);
                return true;
            }
        }

        // Record / update the last-seen timestamp.
        _lastSeen[epc] = now;
        return false;
    }

    /// <summary>
    /// Removes entries older than the suppression window to prevent unbounded
    /// memory growth. Called periodically by <see cref="Worker"/>.
    /// </summary>
    public int PurgeStaleEntries()
    {
        var cutoff = DateTimeOffset.UtcNow - _window;
        var removed = 0;

        foreach (var kvp in _lastSeen)
        {
            if (kvp.Value < cutoff && _lastSeen.TryRemove(kvp.Key, out _))
                removed++;
        }

        if (removed > 0)
            _logger.LogDebug("Purged {Count} stale deduplication entries.", removed);

        return removed;
    }
}
