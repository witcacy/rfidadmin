
using MqttServer.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MqttServer.Services;

public interface IMqttPublisher
{
    Task PublishRfidReadingAsync(RfidReading reading, CancellationToken cancellationToken = default);
}