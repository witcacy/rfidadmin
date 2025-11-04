
namespace MqttServer.Settings;

public sealed class MqttSettings
{
    public string BrokerHost { get; init; } = "localhost";
    public int BrokerPort { get; init; } = 1883;
    public string ClientId { get; init; } = "mqttserver-rfid-publisher";
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string TopicPrefix { get; init; } = "rfid";
    public bool UseTls { get; init; } = false;
    public bool CleanSession { get; init; } = true;
    public int KeepAliveSeconds { get; init; } = 60;
}