using System.Text.Json;
using Rfid.IngestionService.EventEngine;
using Rfid.IngestionService.Models;

namespace Rfid.IngestionService.TcpIngestion;

/// <summary>
/// Deserialises raw TCP JSON messages into <see cref="RawTcpRfidMessage"/>
/// instances, maps them to canonical <see cref="RfidEvent"/> objects, and
/// forwards them into the <see cref="EventProcessor"/> pipeline.
/// </summary>
public class TcpMessageDispatcher
{
    private readonly ILogger<TcpMessageDispatcher> _logger;
    private readonly EventProcessor _eventProcessor;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TcpMessageDispatcher(
        ILogger<TcpMessageDispatcher> logger,
        EventProcessor eventProcessor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventProcessor = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
    }

    /// <summary>
    /// Parses a single JSON line received from a TCP client, maps it to a
    /// canonical <see cref="RfidEvent"/>, and forwards it to the pipeline.
    /// </summary>
    public async Task DispatchAsync(string json, string clientId, CancellationToken cancellationToken)
    {
        RawTcpRfidMessage? raw;
        try
        {
            raw = JsonSerializer.Deserialize<RawTcpRfidMessage>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[{ClientId}] Failed to deserialise TCP message.", clientId);
            return;
        }

        if (raw is null || string.IsNullOrWhiteSpace(raw.Epc))
        {
            _logger.LogDebug("[{ClientId}] Ignoring empty or EPC-less TCP message.", clientId);
            return;
        }

        var rfidEvent = new RfidEvent
        {
            Epc = raw.Epc,
            ReaderId = string.IsNullOrWhiteSpace(raw.ReaderName) ? clientId : raw.ReaderName,
            AntennaPort = raw.AntennaPort,
            Rssi = raw.Rssi,
            Timestamp = raw.Timestamp != default ? raw.Timestamp : DateTimeOffset.UtcNow,
            SourceProtocol = "TCP",
            ChannelIndex = raw.ChannelIndex is > 0 ? (ushort)raw.ChannelIndex.Value : null,
            TagSeenCount = raw.ReadCount > 0 ? (ushort)raw.ReadCount : null
        };

        _logger.LogDebug(
            "[{ClientId}] Dispatching TCP event – EPC: {Epc}, Reader: {Reader}, Antenna: {Antenna}.",
            clientId, rfidEvent.Epc, rfidEvent.ReaderId, rfidEvent.AntennaPort);

        try
        {
            await _eventProcessor.ProcessAsync(rfidEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ClientId}] Error processing TCP event for EPC {Epc}.", clientId, rfidEvent.Epc);
        }
    }
}
