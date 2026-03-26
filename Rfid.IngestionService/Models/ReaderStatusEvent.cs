namespace Rfid.IngestionService.Models;

/// <summary>
/// Represents a reader connection state change, published to SignalR and MQTT
/// for dashboard and external monitoring.
/// </summary>
public class ReaderStatusEvent
{
    /// <summary>Logical identifier of the reader.</summary>
    public string ReaderId { get; set; } = string.Empty;

    /// <summary>Previous connection state name.</summary>
    public string PreviousState { get; set; } = string.Empty;

    /// <summary>New connection state name.</summary>
    public string NewState { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the state transition.</summary>
    public DateTimeOffset Timestamp { get; set; }
}
