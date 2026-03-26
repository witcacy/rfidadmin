using Rfid.IngestionService.Models;

namespace Rfid.IngestionService.Persistence;

/// <summary>
/// Persists processed RFID events into SQL Server.
/// </summary>
public class EventRepository
{
    private readonly ILogger<EventRepository> _logger;

    public EventRepository(ILogger<EventRepository> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Saves an RFID event to the database.
    /// </summary>
    public Task SaveAsync(RfidEvent rfidEvent, CancellationToken cancellationToken)
    {
        // TODO: Insert the event into the SQL Server database.
        _logger.LogDebug("Persisting event for EPC {Epc}", rfidEvent.Epc);
        return Task.CompletedTask;
    }
}
