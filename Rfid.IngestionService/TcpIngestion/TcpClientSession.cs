using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Rfid.IngestionService.TcpIngestion;

/// <summary>
/// Handles a single TCP client connection. Reads delimiter-separated
/// messages from the stream and forwards each message to the
/// <see cref="TcpMessageDispatcher"/>.
/// </summary>
public class TcpClientSession : IDisposable
{
    private readonly TcpClient _tcpClient;
    private readonly TcpMessageDispatcher _dispatcher;
    private readonly ILogger<TcpClientSession> _logger;
    private readonly string _sessionId;
    private readonly string _delimiter;
    private readonly int _bufferSize;
    private bool _disposed;

    public TcpClientSession(
        TcpClient tcpClient,
        TcpMessageDispatcher dispatcher,
        ILogger<TcpClientSession> logger,
        string delimiter,
        int bufferSize)
    {
        _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _delimiter = delimiter;
        _bufferSize = bufferSize;
        _sessionId = _tcpClient.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();
    }

    /// <summary>Logical identifier for this session (remote endpoint).</summary>
    public string SessionId => _sessionId;

    /// <summary>
    /// Reads messages from the TCP stream until the client disconnects or
    /// cancellation is requested.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{SessionId}] TCP client session started.", _sessionId);

        try
        {
            using var reader = new StreamReader(
                _tcpClient.GetStream(),
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: _bufferSize,
                leaveOpen: true);

            var sb = new StringBuilder();
            var buffer = new char[_bufferSize];

            while (!cancellationToken.IsCancellationRequested)
            {
                int charsRead = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);

                if (charsRead == 0)
                {
                    _logger.LogInformation("[{SessionId}] Client disconnected.", _sessionId);
                    break;
                }

                sb.Append(buffer, 0, charsRead);

                // Extract complete messages separated by the delimiter.
                string accumulated = sb.ToString();
                int delimIndex;
                while ((delimIndex = accumulated.IndexOf(_delimiter, StringComparison.Ordinal)) >= 0)
                {
                    var message = accumulated[..delimIndex].Trim();
                    accumulated = accumulated[(delimIndex + _delimiter.Length)..];

                    if (message.Length > 0)
                    {
                        await _dispatcher.DispatchAsync(message, _sessionId, cancellationToken);
                    }
                }

                sb.Clear();
                sb.Append(accumulated);
            }

            // Process any remaining data that was not terminated by the delimiter.
            var remaining = sb.ToString().Trim();
            if (remaining.Length > 0)
            {
                await _dispatcher.DispatchAsync(remaining, _sessionId, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown.
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "[{SessionId}] IO error during TCP session.", _sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{SessionId}] Unexpected error during TCP session.", _sessionId);
        }
        finally
        {
            _logger.LogInformation("[{SessionId}] TCP client session ended.", _sessionId);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _tcpClient.Dispose();
        _disposed = true;
    }
}
