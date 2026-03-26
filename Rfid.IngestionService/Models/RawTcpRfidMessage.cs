namespace Rfid.IngestionService.Models;

/// <summary>
/// Represents a raw RFID message received over a TCP connection.
/// The JSON payload is expected to match the format produced by
/// Zebra FX9600 readers or compatible emulators.
/// </summary>
public sealed class RawTcpRfidMessage
{
    /// <summary>EPC (Electronic Product Code) of the tag, hex-encoded.</summary>
    public string Epc { get; set; } = string.Empty;

    /// <summary>Antenna port number that detected the tag.</summary>
    public int AntennaPort { get; set; }

    /// <summary>RSSI value in dBm.</summary>
    public double Rssi { get; set; }

    /// <summary>UTC timestamp of the read.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Number of times the tag was read in this cycle.</summary>
    public int ReadCount { get; set; }

    /// <summary>Hostname or identifier of the reader device.</summary>
    public string ReaderName { get; set; } = string.Empty;

    /// <summary>Unique TID (Tag Identifier) if available.</summary>
    public string? Tid { get; set; }

    /// <summary>Phase angle of the tag response in degrees.</summary>
    public double? PhaseAngle { get; set; }

    /// <summary>Channel frequency index used during the read.</summary>
    public int? ChannelIndex { get; set; }
}
