namespace Rfid.IngestionService.TcpIngestion;

/// <summary>
/// Configuration options for the TCP ingestion listener.
/// Bound from the "TcpIngestion" section in appsettings.json.
/// </summary>
public sealed class TcpIngestionOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "TcpIngestion";

    /// <summary>Whether the TCP ingestion listener is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>IP address to listen on. Defaults to all interfaces.</summary>
    public string ListenIp { get; set; } = "0.0.0.0";

    /// <summary>TCP port to listen on.</summary>
    public int Port { get; set; } = 9000;

    /// <summary>Size of the read buffer per client session (bytes).</summary>
    public int BufferSize { get; set; } = 4096;

    /// <summary>Maximum number of concurrent TCP client connections.</summary>
    public int MaxClients { get; set; } = 50;

    /// <summary>
    /// Delimiter that separates individual messages in the TCP stream.
    /// Defaults to newline.
    /// </summary>
    public string MessageDelimiter { get; set; } = "\n";
}
