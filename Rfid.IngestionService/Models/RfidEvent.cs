namespace Rfid.IngestionService.Models;

/// <summary>
/// Canonical RFID event model used throughout the ingestion service.
/// </summary>
public class RfidEvent
{
    /// <summary>Electronic Product Code read from the tag.</summary>
    public string Epc { get; set; } = string.Empty;

    /// <summary>Identifier of the reader that produced this read.</summary>
    public string ReaderId { get; set; } = string.Empty;

    /// <summary>Antenna port number on the reader.</summary>
    public int AntennaPort { get; set; }

    /// <summary>Received signal strength indicator (dBm).</summary>
    public double Rssi { get; set; }

    /// <summary>UTC timestamp of the tag read.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Logical zone resolved from the reader/antenna combination.</summary>
    public string? Zone { get; set; }

    /// <summary>Source protocol that produced this event (e.g. "LLRP", "TCP").</summary>
    public string? SourceProtocol { get; set; }

    /// <summary>LLRP channel index on which the tag was observed.</summary>
    public ushort? ChannelIndex { get; set; }

    /// <summary>Number of times the tag was seen during the inventory round.</summary>
    public ushort? TagSeenCount { get; set; }

    /// <summary>LLRP AccessSpec ID if an access operation was executed.</summary>
    public uint? AccessSpecId { get; set; }

    /// <summary>LLRP InventoryParameterSpec ID that produced this read.</summary>
    public ushort? InventoryParameterSpecId { get; set; }

    /// <summary>LLRP OpSpec result status code, or <c>null</c> if no access operation.</summary>
    public ushort? OpSpecResultStatus { get; set; }
}
