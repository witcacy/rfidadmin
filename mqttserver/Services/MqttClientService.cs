//using System.Text;
//using System.Text.Json;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using MQTTnet;
//using MQTTnet.Client;
//using MQTTnet.Formatter;
//using MQTTnet.Protocol;
//using MqttServer.Models;
//using MqttServer.Settings;

//namespace MqttServer.Services;

//public sealed class MqttClientService : BackgroundService, IMqttPublisher
//{
//    private readonly ILogger<MqttClientService> _logger;
//    private readonly MqttSettings _settings;
//    private IMqttClient? _client;
//    private readonly SemaphoreSlim _connectionLock = new(1, 1);
//    private readonly MqttFactory _factory = new();

//    public MqttClientService(IOptions<MqttSettings> options, ILogger<MqttClientService> logger)
//    {
//        _logger = logger;
//        _settings = options.Value;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                if (!IsConnected())
//                {
//                    await ConnectAsync(stoppingToken);
//                }

//                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
//            }
//            catch (OperationCanceledException) { break; }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in MQTT connection loop, retrying in 5s");
//                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
//            }
//        }
//    }

//    private bool IsConnected() => _client?.IsConnected ?? false;

//    private async Task ConnectAsync(CancellationToken cancellationToken)
//    {
//        await _connectionLock.WaitAsync(cancellationToken);
//        try
//        {
//            if (IsConnected()) return;

//            _client = _factory.CreateMqttClient();

//            _client.ConnectedAsync += args =>
//            {
//                _logger.LogInformation("Connected to MQTT broker at {Host}:{Port}", _settings.BrokerHost, _settings.BrokerPort);
//                return Task.CompletedTask;
//            };

//            _client.DisconnectedAsync += async args =>
//            {
//                _logger.LogWarning("Disconnected from MQTT broker. Reconnecting in 3s...");
//                await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);

//                try
//                {
//                    if (_client != null)
//                    {
//                        await _client.ConnectAsync(BuildClientOptions(), CancellationToken.None);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Reconnect failed");
//                }
//            };

//            _client.ApplicationMessageReceivedAsync += args =>
//            {
//                var topic = args.ApplicationMessage?.Topic ?? string.Empty;
//                var payload = args.ApplicationMessage?.PayloadSegment.Array == null
//                    ? string.Empty
//                    : Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

//                _logger.LogInformation("Received message on topic {Topic}: {Payload}", topic, payload);
//                return Task.CompletedTask;
//            };

//            await _client.ConnectAsync(BuildClientOptions(), cancellationToken);
//        }
//        finally
//        {
//            _connectionLock.Release();
//        }
//    }

//    private MqttClientOptions BuildClientOptions()
//    {
//        var builder = new MqttClientOptionsBuilder()
//            .WithClientId(_settings.ClientId)
//            .WithTcpServer(_settings.BrokerHost, _settings.BrokerPort)
//            .WithCleanSession(_settings.CleanSession)
//            .WithKeepAlivePeriod(TimeSpan.FromSeconds(_settings.KeepAliveSeconds))
//            .WithProtocolVersion(MqttProtocolVersion.V311); // Usa V500 si tu broker lo soporta

//        if (!string.IsNullOrEmpty(_settings.Username))
//        {
//            builder = builder.WithCredentials(_settings.Username, _settings.Password ?? string.Empty);
//        }

//        if (_settings.UseTls)
//        {
//            builder = builder.WithTlsOptions(o =>
//            {
//                o.UseTls = true;
//                o.AllowUntrustedCertificates = true;
//                o.IgnoreCertificateChainErrors = true;
//                o.IgnoreCertificateRevocationErrors = true;
//            });
//        }

//        return builder.Build();
//    }

//    public async Task PublishRfidReadingAsync(RfidReading reading, CancellationToken cancellationToken = default)
//    {
//        if (_client == null || !_client.IsConnected)
//        {
//            _logger.LogWarning("Client not connected, attempting connect before publishing");
//            await ConnectAsync(cancellationToken);
//        }

//        var topic = $"{_settings.TopicPrefix}/readings";
//        var payload = JsonSerializer.Serialize(reading);

//        var message = new MqttApplicationMessageBuilder()
//            .WithTopic(topic)
//            .WithPayload(payload)
//            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
//            .WithRetainFlag(false)
//            .Build();

//        if (_client != null)
//        {
//            await _client.PublishAsync(message, cancellationToken);
//            _logger.LogInformation("Published RFID reading to {Topic} TagId={Tag}", topic, reading.TagId);
//        }
//        else
//        {
//            _logger.LogError("MQTT client is null after connect attempt");
//        }
//    }

//    public override async Task StopAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("Stopping MQTT client service");
//        if (_client != null && _client.IsConnected)
//        {
//            try
//            {
//                await _client.DisconnectAsync();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogWarning(ex, "Error while disconnecting MQTT client");
//            }
//        }
//        await base.StopAsync(cancellationToken);
//    }
//}
