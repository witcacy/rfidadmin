
using System;

namespace MqttServer.Models;

public sealed class RfidReading
{
    public string TagId { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? AntennaId { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}