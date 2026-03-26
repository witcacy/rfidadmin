using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using Rfid.IngestionService.Models;


namespace Rfid.IngestionService.Distribution.Mqtt;

/// <summary>
/// Publishes processed RFID events to an MQTT broker (e.g. Mosquitto).
/// This class is a pure distribution component — no filtering, transformation, or business logic.
/// </summary>
public class MqttPublisher : IAsyncDisposable
{
    private readonly ILogger<MqttPublisher> _logger;
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _mqttOptions;
    private readonly string _topic;
    private readonly string _readerStatusTopic;

    public MqttPublisher(ILogger<MqttPublisher> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Read MQTT broker settings from configuration.
        var host = configuration["Mqtt:Broker:Host"]
            ?? throw new InvalidOperationException("Configuration value 'Mqtt:Broker:Host' is required.");
        var port = int.Parse(configuration["Mqtt:Broker:Port"] ?? "1883");
        var clientId = configuration["Mqtt:Broker:ClientId"] ?? $"RfidIngestion-{Environment.MachineName}";
        _topic = configuration["Mqtt:Topics:TagDetected"]
            ?? throw new InvalidOperationException("Configuration value 'Mqtt:Topics:TagDetected' is required.");
        _readerStatusTopic = configuration["Mqtt:Topics:ReaderStatus"] ?? "rfid/events/readerStatus";

        var factory = new MqttClientFactory();
        _mqttClient = factory.CreateMqttClient();

        _mqttOptions = factory.CreateClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId(clientId)
            // TODO: Configure TLS/SSL for secure connections.
            // TODO: Configure username/password credentials.
            .Build();

        // Log connection state changes.
        _mqttClient.ConnectedAsync += args =>
        {
            _logger.LogInformation("MQTT client connected to {Host}:{Port}.", host, port);
            return Task.CompletedTask;
        };

        _mqttClient.DisconnectedAsync += args =>
        {
            _logger.LogWarning(args.Exception, "MQTT client disconnected. Reason: {Reason}.",
                args.ReasonString);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Connects to the MQTT broker.
    /// Should be called once during application startup.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connecting MQTT client...");
        await _mqttClient.ConnectAsync(_mqttOptions, cancellationToken);
    }

    /// <summary>
    /// Publishes the event to the configured MQTT topic as a JSON payload.
    /// </summary>
    public async Task PublishAsync(RfidEvent rfidEvent, CancellationToken cancellationToken)
    {
        if (!_mqttClient.IsConnected)
        {
            _logger.LogWarning("MQTT client is not connected. Skipping publish for EPC {Epc}.", rfidEvent.Epc);
            return;
        }

        var payload = JsonSerializer.Serialize(rfidEvent);

        // TODO: Configure QoS level (AtMostOnce, AtLeastOnce, ExactlyOnce).
        // TODO: Configure retain flag based on requirements.
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(_topic)
            .WithPayload(payload)
            .Build();

        _logger.LogDebug("Publishing event to MQTT topic '{Topic}' for EPC {Epc}.", _topic, rfidEvent.Epc);
        await _mqttClient.PublishAsync(message, cancellationToken);
    }

    /// <summary>
    /// Publishes a reader status event to the reader-status MQTT topic.
    /// </summary>
    public async Task PublishReaderStatusAsync(ReaderStatusEvent statusEvent, CancellationToken cancellationToken)
    {
        if (!_mqttClient.IsConnected)
        {
            _logger.LogWarning("MQTT client is not connected. Skipping reader status publish for {ReaderId}.", statusEvent.ReaderId);
            return;
        }

        var payload = JsonSerializer.Serialize(statusEvent);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(_readerStatusTopic)
            .WithPayload(payload)
            .Build();

        _logger.LogDebug("Publishing reader status to MQTT topic '{Topic}' for {ReaderId}.", _readerStatusTopic, statusEvent.ReaderId);
        await _mqttClient.PublishAsync(message, cancellationToken);
    }

    /// <summary>
    /// Gracefully disconnects from the MQTT broker.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient.IsConnected)
        {
            _logger.LogInformation("Disconnecting MQTT client...");
            var disconnectOptions = new MqttClientDisconnectOptionsBuilder()
                .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                .Build();
            await _mqttClient.DisconnectAsync(disconnectOptions, cancellationToken);
            _logger.LogInformation("MQTT client disconnected.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _mqttClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
