using Microsoft.Extensions.Configuration;
using Rfid.IngestionService.Models;

namespace Rfid.IngestionService.EventEngine;

/// <summary>
/// Resolves a logical zone (e.g. "Tool Crib", "Exit Gate") based on
/// the reader identity and antenna port that produced the tag read.
/// <para>
/// Zone mappings are loaded from the <c>ZoneMappings</c> section in
/// <c>appsettings.json</c>. Each entry maps a composite key
/// <c>"{ReaderId}:{AntennaPort}"</c> to a zone name. Example:
/// <code>
/// "ZoneMappings": {
///   "Dock-Door-1:1": "Tool Crib",
///   "Dock-Door-1:2": "Exit Gate"
/// }
/// </code>
/// When no mapping is found the zone is set to <c>null</c>.
/// </para>
/// </summary>
public class ZoneResolver
{
    private readonly ILogger<ZoneResolver> _logger;

    /// <summary>
    /// In-memory zone lookup: "{ReaderId}:{AntennaPort}" ? zone name.
    /// </summary>
    private readonly Dictionary<string, string> _zoneMappings;

    public ZoneResolver(ILogger<ZoneResolver> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _zoneMappings = configuration?.GetSection("ZoneMappings")
            .Get<Dictionary<string, string>>()
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("Zone resolver loaded {Count} mapping(s).", _zoneMappings.Count);
    }

    /// <summary>
    /// Enriches the event with the resolved zone information.
    /// Sets <see cref="RfidEvent.Zone"/> when a mapping exists; otherwise leaves it <c>null</c>.
    /// </summary>
    public void Resolve(RfidEvent rfidEvent)
    {
        var key = BuildKey(rfidEvent.ReaderId, rfidEvent.AntennaPort);

        if (_zoneMappings.TryGetValue(key, out var zone))
        {
            rfidEvent.Zone = zone;
            _logger.LogDebug("[{ReaderId}] Antenna {AntennaPort} ? Zone '{Zone}'.",
                rfidEvent.ReaderId, rfidEvent.AntennaPort, zone);
        }
        else
        {
            rfidEvent.Zone = null;
            _logger.LogDebug("[{ReaderId}] No zone mapping for antenna {AntennaPort}.",
                rfidEvent.ReaderId, rfidEvent.AntennaPort);

            // TODO: Fall back to database lookup or external zone service.
        }
    }

    /// <summary>
    /// Builds the composite lookup key for a reader/antenna combination.
    /// </summary>
    private static string BuildKey(string readerId, int antennaPort)
        => $"{readerId}:{antennaPort}";
}
