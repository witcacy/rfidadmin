using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace Rfid.IngestionService.TcpIngestion;

/// <summary>
/// Background service that listens for incoming TCP connections from RFID
/// readers or emulators and creates a <see cref="TcpClientSession"/> for
/// each accepted client.
/// </summary>
public class TcpListenerService : BackgroundService
{
    private readonly ILogger<TcpListenerService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TcpIngestionOptions _options;
    private readonly TcpMessageDispatcher _dispatcher;
    private readonly ConcurrentDictionary<string, Task> _activeSessions = new();
    private TcpListener? _listener;

    public TcpListenerService(
        ILogger<TcpListenerService> logger,
        ILoggerFactory loggerFactory,
        IOptions<TcpIngestionOptions> options,
        TcpMessageDispatcher dispatcher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("TCP ingestion is disabled via configuration.");
            return;
        }

        var ip = IPAddress.Parse(_options.ListenIp);
        _listener = new TcpListener(ip, _options.Port);

        try
        {
            _listener.Start();
            _logger.LogInformation(
                "TCP ingestion listener started on {Ip}:{Port} (max clients: {Max}).",
                _options.ListenIp, _options.Port, _options.MaxClients);
        }
        catch (SocketException ex)
        {
            _logger.LogCritical(ex, "Failed to start TCP listener on {Ip}:{Port}.", _options.ListenIp, _options.Port);
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient tcpClient;
                try
                {
                    tcpClient = await _listener.AcceptTcpClientAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (_activeSessions.Count >= _options.MaxClients)
                {
                    _logger.LogWarning(
                        "Max client limit ({Max}) reached. Rejecting connection from {Remote}.",
                        _options.MaxClients,
                        tcpClient.Client.RemoteEndPoint);
                    tcpClient.Dispose();
                    continue;
                }

                var session = new TcpClientSession(
                    tcpClient,
                    _dispatcher,
                    _loggerFactory.CreateLogger<TcpClientSession>(),
                    _options.MessageDelimiter,
                    _options.BufferSize);

                var sessionTask = RunSessionAsync(session, stoppingToken);
                _activeSessions[session.SessionId] = sessionTask;

                _logger.LogInformation(
                    "Accepted TCP client {SessionId}. Active sessions: {Count}.",
                    session.SessionId, _activeSessions.Count);
            }
        }
        finally
        {
            _listener.Stop();
            _logger.LogInformation("TCP listener stopped. Waiting for {Count} active sessions to finish.", _activeSessions.Count);

            // Wait for all active sessions to complete.
            await Task.WhenAll(_activeSessions.Values);
            _logger.LogInformation("All TCP client sessions ended.");
        }
    }

    /// <summary>
    /// Runs a single client session and removes it from the active set on completion.
    /// </summary>
    private async Task RunSessionAsync(TcpClientSession session, CancellationToken cancellationToken)
    {
        try
        {
            await session.RunAsync(cancellationToken);
        }
        finally
        {
            _activeSessions.TryRemove(session.SessionId, out _);
            session.Dispose();
            _logger.LogDebug("Session {SessionId} removed. Active sessions: {Count}.",
                session.SessionId, _activeSessions.Count);
        }
    }
}
