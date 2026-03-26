using Rfid.IngestionService;
using Rfid.IngestionService.Distribution.Mqtt;
using Rfid.IngestionService.Distribution.SignalR;
using Rfid.IngestionService.EventEngine;
using Rfid.IngestionService.Llrp;
using Rfid.IngestionService.Normalization;
using Rfid.IngestionService.Persistence;
using Rfid.IngestionService.TcpIngestion;

var builder = Host.CreateApplicationBuilder(args);

// Run as a Windows Service when deployed.
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Rfid.IngestionService";
});

// LLRP layer
builder.Services.AddSingleton<LlrpConnectionManager>();
builder.Services.AddTransient<LlrpClient>();
builder.Services.AddSingleton<RospecBuilder>();

// Normalization
builder.Services.AddSingleton<RfidEventMapper>();

// Event engine
builder.Services.AddSingleton<EventProcessor>();
builder.Services.AddSingleton<DeduplicationService>();
builder.Services.AddSingleton<ZoneResolver>();

// Distribution
builder.Services.AddSingleton<SignalRPublisher>();
builder.Services.AddSingleton<MqttPublisher>();

// Persistence
builder.Services.AddSingleton<EventRepository>();

// TCP ingestion layer
builder.Services.Configure<TcpIngestionOptions>(
    builder.Configuration.GetSection(TcpIngestionOptions.SectionName));
builder.Services.AddSingleton<TcpMessageDispatcher>();
builder.Services.AddHostedService<TcpListenerService>();

// Hosted worker (LLRP connections + health checks)
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
