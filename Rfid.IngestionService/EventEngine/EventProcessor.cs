using Rfid.IngestionService.Distribution.Mqtt;
using Rfid.IngestionService.Distribution.SignalR;
using Rfid.IngestionService.Models;
using Rfid.IngestionService.Persistence;

namespace Rfid.IngestionService.EventEngine;

/// <summary>
/// Central entry point for all normalised RFID events.
/// <para>
/// Pipeline order:
/// <list type="number">
///   <item>Deduplication — suppress repeated reads within the configured window.</item>
///   <item>Zone resolution — enrich the event with a logical zone name.</item>
///   <item>Distribution — forward to MQTT, SignalR, and persistence.</item>
/// </list>
/// </para>
/// </summary>
public class EventProcessor
{
    private readonly ILogger<EventProcessor> _logger;
    private readonly DeduplicationService _deduplicationService;
    private readonly ZoneResolver _zoneResolver;
    private readonly MqttPublisher _mqttPublisher;
    private readonly SignalRPublisher _signalRPublisher;
    private readonly EventRepository _eventRepository;

    public EventProcessor(
        ILogger<EventProcessor> logger,
        DeduplicationService deduplicationService,
        ZoneResolver zoneResolver,
        MqttPublisher mqttPublisher,
        SignalRPublisher signalRPublisher,
        EventRepository eventRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deduplicationService = deduplicationService ?? throw new ArgumentNullException(nameof(deduplicationService));
        _zoneResolver = zoneResolver ?? throw new ArgumentNullException(nameof(zoneResolver));
        _mqttPublisher = mqttPublisher ?? throw new ArgumentNullException(nameof(mqttPublisher));
        _signalRPublisher = signalRPublisher ?? throw new ArgumentNullException(nameof(signalRPublisher));
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
    }

    /// <summary>
    /// Processes a single normalised RFID event through the full pipeline.
    /// </summary>
    public async Task ProcessAsync(RfidEvent rfidEvent, CancellationToken cancellationToken)
    {
        // ?? Step 1: Deduplication ??????????????????????????????????????
        if (_deduplicationService.IsDuplicate(rfidEvent))
        {
            _logger.LogDebug("Event for EPC {Epc} suppressed by deduplication.", rfidEvent.Epc);
            return;
        }

        // ?? Step 2: Zone resolution ???????????????????????????????????
        _zoneResolver.Resolve(rfidEvent);

        _logger.LogInformation(
            "Event accepted — EPC: {Epc}, Reader: {ReaderId}, Antenna: {Antenna}, Zone: {Zone}",
            rfidEvent.Epc, rfidEvent.ReaderId, rfidEvent.AntennaPort, rfidEvent.Zone ?? "(unknown)");

        // ?? Step 3: Distribution ??????????????????????????????????????
        // Forward the enriched event to all downstream consumers.
        // Individual failures are logged but do not block the other targets.

        await PublishSafeAsync(
            "MQTT", () => _mqttPublisher.PublishAsync(rfidEvent, cancellationToken));

        await PublishSafeAsync(
            "SignalR", () => _signalRPublisher.PublishAsync(rfidEvent, cancellationToken));

        await PublishSafeAsync(
            "Persistence", () => _eventRepository.SaveAsync(rfidEvent, cancellationToken));

        // TODO: Add additional distribution targets (e.g. Azure Event Hub, webhook) here.
    }

    /// <summary>
    /// Invokes a distribution target and swallows exceptions so that one
    /// failing target does not prevent delivery to the others.
    /// </summary>
    private async Task PublishSafeAsync(string targetName, Func<Task> publish)
    {
        try
        {
            await publish();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver event to {Target}.", targetName);
            // TODO: Implement retry / dead-letter strategy per target.
        }
    }
}
