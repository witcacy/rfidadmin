using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MqttServer.Services;
using MqttServer.Settings;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<MqttSettings>(context.Configuration.GetSection("Mqtt"));

        // register a single instance that is both IHostedService and IMqttPublisher
        services.AddSingleton<MqttClientService>();
        services.AddSingleton<IMqttPublisher>(sp => sp.GetRequiredService<MqttClientService>());
        services.AddHostedService(sp => sp.GetRequiredService<MqttClientService>());
    })
    .Build();

await host.RunAsync();
