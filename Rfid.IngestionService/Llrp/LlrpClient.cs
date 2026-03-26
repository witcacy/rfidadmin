using System.Buffers.Binary;
using System.Net.Sockets;
using Rfid.IngestionService.EventEngine;
using Rfid.IngestionService.Normalization;

namespace Rfid.IngestionService.Llrp;

/// <summary>
/// Tracks the lifecycle state of a single LLRP reader connection.
/// </summary>
public enum ReaderState
{
    /// <summary>No TCP connection to the reader.</summary>
    Disconnected,

    /// <summary>TCP connection attempt is in progress.</summary>
    Connecting,

    /// <summary>TCP connection established and ROSpec applied; reader is inventorying.</summary>
    Connected,

    /// <summary>An error occurred (connection lost, IO fault, etc.).</summary>
    Error
}

/// <summary>
/// Manages a single LLRP TCP connection to one RFID reader (e.g. Zebra FX9600).
/// After connecting, applies a ROSpec (ADD ? ENABLE ? START) so the reader
/// begins tag inventory. Incoming data is scanned for LLRP RO_ACCESS_REPORT
/// (TagReport) messages; each tag read is mapped to a canonical
/// <see cref="Models.RfidEvent"/> via <see cref="RfidEventMapper"/> and forwarded
/// to the <see cref="EventProcessor"/> pipeline.
/// <para>
/// Full LLRP binary decoding is not yet implemented. The current version uses
/// placeholder parsing with clear method boundaries where real decoding will
/// be added.
/// </para>
/// </summary>
public class LlrpClient : IDisposable
{
    /// <summary>Default LLRP TCP port defined by the LLRP specification.</summary>
    public const int DefaultPort = 5084;

    /// <summary>Size of the buffer used by the receive loop.</summary>
    private const int ReceiveBufferSize = 4096;

    /// <summary>
    /// LLRP message type value for RO_ACCESS_REPORT (decimal 61 / 0x003D).
    /// Used by the placeholder message-type detection logic.
    /// </summary>
    private const ushort LlrpMessageTypeRoAccessReport = 61;

    private readonly ILogger<LlrpClient> _logger;
    private readonly string _readerId;
    private readonly string _readerIp;
    private readonly int _port;
    private readonly RfidEventMapper? _eventMapper;
    private readonly EventProcessor? _eventProcessor;

    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private CancellationTokenSource? _listenCts;
    private Task? _listenTask;
    private bool _disposed;

    /// <summary>Accumulates bytes from the TCP stream for LLRP message framing.</summary>
    private readonly MemoryStream _frameBuffer = new();

    /// <summary>Timeout for reading a response after sending an LLRP command.</summary>
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The ROSpec ID that was applied to the reader during this connection,
    /// or <c>null</c> if no ROSpec has been applied yet.
    /// </summary>
    private uint? _activeRoSpecId;

    /// <summary>
    /// Guards against applying the ROSpec more than once per connection.
    /// </summary>
    private bool _roSpecApplied;

    /// <summary>
    /// Current lifecycle state of this reader connection.
    /// </summary>
    private ReaderState _state = ReaderState.Disconnected;

    /// <summary>
    /// Gets the current lifecycle state of this reader connection.
    /// </summary>
    public ReaderState CurrentState => _state;

    /// <summary>
    /// Raised when the reader connection state changes.
    /// Parameters: ReaderId, previous state, new state.
    /// </summary>
    public event Action<string, ReaderState, ReaderState>? StateChanged;

    /// <summary>
    /// Raised when raw data is received from the reader.
    /// The byte array contains the unprocessed payload read from the TCP stream.
    /// </summary>
    public event Action<string, byte[]>? DataReceived;

    /// <summary>
    /// Creates a new <see cref="LlrpClient"/> targeting a specific reader.
    /// </summary>
    /// <param name="readerId">Logical identifier for the reader (used in logs and events).</param>
    /// <param name="readerIp">IP address or hostname of the RFID reader.</param>
    /// <param name="port">TCP port; defaults to <see cref="DefaultPort"/> (5084).</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="eventMapper">Maps raw LLRP tag data into canonical <see cref="Models.RfidEvent"/> instances.</param>
    /// <param name="eventProcessor">Pipeline entry-point that receives normalised events.</param>
    public LlrpClient(
        string readerId,
        string readerIp,
        int port,
        ILogger<LlrpClient> logger,
        RfidEventMapper eventMapper,
        EventProcessor eventProcessor)
    {
        _readerId = readerId ?? throw new ArgumentNullException(nameof(readerId));
        _readerIp = readerIp ?? throw new ArgumentNullException(nameof(readerIp));
        _port = port;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventMapper = eventMapper ?? throw new ArgumentNullException(nameof(eventMapper));
        _eventProcessor = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
    }

    /// <inheritdoc cref="LlrpClient(string, string, int, ILogger{LlrpClient}, RfidEventMapper, EventProcessor)"/>
    public LlrpClient(
        string readerId,
        string readerIp,
        ILogger<LlrpClient> logger,
        RfidEventMapper eventMapper,
        EventProcessor eventProcessor)
        : this(readerId, readerIp, DefaultPort, logger, eventMapper, eventProcessor)
    {
    }

    /// <summary>
    /// Creates a new <see cref="LlrpClient"/> without event-pipeline wiring.
    /// TagReport messages will be surfaced via <see cref="DataReceived"/> only.
    /// </summary>
    public LlrpClient(string readerId, string readerIp, int port, ILogger<LlrpClient> logger)
        : this(readerId, readerIp, port, logger, null!, null!)
    {
        // Null-out the pipeline fields so ProcessTagReportAsync falls back to DataReceived.
        _eventMapper = null;
        _eventProcessor = null;
    }

    /// <inheritdoc cref="LlrpClient(string, string, int, ILogger{LlrpClient})"/>
    public LlrpClient(string readerId, string readerIp, ILogger<LlrpClient> logger)
        : this(readerId, readerIp, DefaultPort, logger)
    {
    }

    /// <summary>
    /// Connects to the reader, applies the default ROSpec, and starts the
    /// background receive loop.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ConnectAsync(cancellationToken);

            // -- ROSpec application (must happen after a successful connection) --
            await ApplyRoSpecAsync(cancellationToken);

            _listenCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listenTask = ListenAsync(_listenCts.Token);

            _logger.LogInformation("[{ReaderId}] LLRP client started.", _readerId);
        }
        catch
        {
            TransitionState(ReaderState.Error);
            throw;
        }
    }

    /// <summary>
    /// Gracefully stops the ROSpec on the reader, cancels the receive loop,
    /// and disconnects.
    /// </summary>
    public async Task StopAsync()
    {
        _logger.LogInformation("[{ReaderId}] Stopping LLRP client…", _readerId);

        // -- Tear down the active ROSpec before disconnecting --
        await TearDownRoSpecAsync();

        if (_listenCts is not null)
        {
            await _listenCts.CancelAsync();

            if (_listenTask is not null)
            {
                try
                {
                    await _listenTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown.
                }
            }

            _listenCts.Dispose();
            _listenCts = null;
        }

        Disconnect();
        TransitionState(ReaderState.Disconnected);
        _logger.LogInformation("[{ReaderId}] LLRP client stopped.", _readerId);
    }

    /// <summary>
    /// Opens the TCP connection to the reader.
    /// </summary>
    internal async Task ConnectAsync(CancellationToken cancellationToken)
    {
        TransitionState(ReaderState.Connecting);

        _logger.LogInformation("[{ReaderId}] Connecting to reader at {Ip}:{Port}...", _readerId, _readerIp, _port);

        _tcpClient = new TcpClient();

        try
        {
            await _tcpClient.ConnectAsync(_readerIp, _port, cancellationToken);
            _stream = _tcpClient.GetStream();

            TransitionState(ReaderState.Connected);
            _logger.LogInformation("[{ReaderId}] Connected to reader at {Ip}:{Port}.", _readerId, _readerIp, _port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ReaderId}] Failed to connect to reader at {Ip}:{Port}.", _readerId, _readerIp, _port);
            TransitionState(ReaderState.Error);
            Disconnect();
            throw;
        }
    }

    /// <summary>
    /// Long-running receive loop that reads raw bytes from the TCP stream,
    /// accumulates them in a framing buffer, extracts complete LLRP messages,
    /// and dispatches each by type.
    /// </summary>
    internal async Task ListenAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[ReceiveBufferSize];
        _logger.LogDebug("[{ReaderId}] Receive loop started.", _readerId);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_stream is null)
                    break;

                int bytesRead = await _stream.ReadAsync(buffer.AsMemory(), cancellationToken);

                if (bytesRead == 0)
                {
                    _logger.LogWarning("[{ReaderId}] Reader closed the connection.", _readerId);
                    TransitionState(ReaderState.Error);
                    break;
                }

                _logger.LogDebug("[{ReaderId}] Received {Bytes} bytes.", _readerId, bytesRead);

                // Append to the framing buffer.
                _frameBuffer.Write(buffer, 0, bytesRead);

                // Extract and process all complete LLRP messages.
                await DrainFrameBufferAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown – not an error.
        }
        catch (IOException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "[{ReaderId}] IO error in receive loop.", _readerId);
            TransitionState(ReaderState.Error);
            // Reconnect is triggered by LlrpConnectionManager via the Error state transition.
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "[{ReaderId}] Unexpected error in receive loop.", _readerId);
            TransitionState(ReaderState.Error);
        }

        _logger.LogDebug("[{ReaderId}] Receive loop ended.", _readerId);
    }

    /// <summary>
    /// Processes all complete LLRP messages currently buffered in <see cref="_frameBuffer"/>.
    /// Handles message framing: each LLRP message starts with a 10-byte header that
    /// includes the total message length. Partial messages remain in the buffer.
    /// </summary>
    private async Task DrainFrameBufferAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var accumulated = _frameBuffer.GetBuffer().AsSpan(0, (int)_frameBuffer.Length);

            // Need at least 10 bytes for the LLRP header.
            if (accumulated.Length < LlrpMessageEncoder.HeaderSize)
                break;

            // Read the message length from the header (bytes 2–5, big-endian uint32).
            var messageLength = BinaryPrimitives.ReadUInt32BigEndian(accumulated[2..]);

            if (messageLength < LlrpMessageEncoder.HeaderSize || messageLength > 1_048_576)
            {
                _logger.LogWarning("[{ReaderId}] Invalid LLRP message length {Length}. Clearing buffer.",
                    _readerId, messageLength);
                _frameBuffer.SetLength(0);
                break;
            }

            if (accumulated.Length < (int)messageLength)
                break; // Incomplete message — wait for more data.

            // Extract the complete message.
            var message = accumulated[..(int)messageLength].ToArray();

            // Compact the buffer: shift remaining bytes to the front.
            var remaining = accumulated[(int)messageLength..];
            _frameBuffer.SetLength(0);
            if (remaining.Length > 0)
                _frameBuffer.Write(remaining);

            // Dispatch the complete message.
            await DispatchMessageAsync(message, cancellationToken);
        }
    }

    /// <summary>
    /// Dispatches a single complete LLRP message by its type.
    /// </summary>
    private async Task DispatchMessageAsync(byte[] message, CancellationToken cancellationToken)
    {
        if (!LlrpMessageEncoder.TryParseHeader(message, out var version, out var messageType,
                out var messageLength, out var messageId))
        {
            _logger.LogWarning("[{ReaderId}] Failed to parse LLRP header.", _readerId);
            return;
        }

        _logger.LogDebug(
            "[{ReaderId}] LLRP message: Type={TypeName} ({Type}), Length={Length}, ID={Id}, Version={Version}.",
            _readerId, LlrpMessageEncoder.GetMessageTypeName(messageType),
            messageType, messageLength, messageId, version);

        switch (messageType)
        {
            case LlrpMessageEncoder.MsgTypeRoAccessReport:
                await ProcessTagReportAsync(message, cancellationToken);
                break;

            case LlrpMessageEncoder.MsgTypeKeepalive:
                _logger.LogDebug("[{ReaderId}] KEEPALIVE received – sending ACK.", _readerId);
                await SendRawAsync(LlrpMessageEncoder.BuildKeepaliveAck(), cancellationToken);
                break;

            case LlrpMessageEncoder.MsgTypeReaderEventNotification:
                _logger.LogInformation("[{ReaderId}] READER_EVENT_NOTIFICATION received.", _readerId);
                DataReceived?.Invoke(_readerId, message);
                break;

            case LlrpMessageEncoder.MsgTypeErrorMessage:
                _logger.LogWarning("[{ReaderId}] ERROR_MESSAGE received from reader.", _readerId);
                if (LlrpMessageEncoder.TryParseResponse(message, out var errResp))
                {
                    _logger.LogWarning("[{ReaderId}] Error status: {Code} – {Desc}.",
                        _readerId, errResp.StatusCode, errResp.ErrorDescription ?? "(no description)");
                }
                DataReceived?.Invoke(_readerId, message);
                break;

            default:
                // Surface all other messages (including response types that may arrive
                // outside the request-response flow) via the raw event.
                DataReceived?.Invoke(_readerId, message);
                break;
        }
    }

    // --------------------------------------------------------------
    // TagReport detection & processing
    // --------------------------------------------------------------

    /// <summary>
    /// Determines whether the given data buffer starts with an LLRP
    /// RO_ACCESS_REPORT message header (message type 61).
    /// Validates the LLRP version and minimum header size.
    /// </summary>
    private static bool IsRoAccessReport(byte[] data)
    {
        if (!LlrpMessageEncoder.TryParseHeader(data, out var version, out var messageType, out _, out _))
            return false;

        // Accept LLRP v1 (v1.0 and v1.1 both use version = 1).
        if (version != LlrpMessageEncoder.LlrpVersion)
            return false;

        return messageType == LlrpMessageTypeRoAccessReport;
    }

    /// <summary>
    /// Processes a single RO_ACCESS_REPORT message by extracting all
    /// TagReportData entries, mapping each to a canonical event, and
    /// forwarding to the <see cref="EventProcessor"/> pipeline.
    /// </summary>
    private async Task ProcessTagReportAsync(byte[] data, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[{ReaderId}] RO_ACCESS_REPORT received ({Bytes} bytes) – extracting tag reads.",
            _readerId, data.Length);

        // When the event pipeline is not wired, fall back to the raw DataReceived event.
        if (_eventMapper is null || _eventProcessor is null)
        {
            _logger.LogDebug(
                "[{ReaderId}] Event pipeline not configured – raising DataReceived for TagReport.",
                _readerId);
            DataReceived?.Invoke(_readerId, data);
            return;
        }

        // Walk the RO_ACCESS_REPORT body and decode each TagReportData entry.
        var tagReads = LlrpMessageEncoder.ExtractTagReads(data);

        if (tagReads.Count == 0)
        {
            _logger.LogDebug("[{ReaderId}] No tag reads found in RO_ACCESS_REPORT.", _readerId);
            return;
        }

        _logger.LogDebug("[{ReaderId}] Extracted {Count} tag read(s).", _readerId, tagReads.Count);

        foreach (var tag in tagReads)
        {
            var rfidEvent = _eventMapper.MapFromLlrpTagReport(
                _readerId,
                tag.Epc,
                tag.AntennaPort,
                tag.TimestampUtc,
                tag.Rssi,
                tag.ChannelIndex,
                tag.TagSeenCount,
                tag.AccessSpecId);

            if (rfidEvent is null)
                continue;

            try
            {
                await _eventProcessor.ProcessAsync(rfidEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{ReaderId}] Error processing tag EPC {Epc}.",
                    _readerId, tag.Epc);
            }
        }
    }

    // --------------------------------------------------------------
    // ROSpec lifecycle – ADD ? ENABLE ? START (setup)
    //                    STOP ? DISABLE        (teardown)
    // --------------------------------------------------------------

    /// <summary>
    /// Builds the default ROSpec and sends the ADD ? ENABLE ? START command
    /// sequence to the reader. This is called once after each successful connection.
    /// </summary>
    private async Task ApplyRoSpecAsync(CancellationToken cancellationToken)
    {
        if (_roSpecApplied)
        {
            _logger.LogDebug("[{ReaderId}] ROSpec already applied – skipping.", _readerId);
            return;
        }

        // Step 1: Build the default ROSpec using RospecBuilder.
        var roSpec = RospecBuilder.BuildDefaultRospec();
        _activeRoSpecId = roSpec.RoSpecId;

        _logger.LogInformation(
            "[{ReaderId}] Applying ROSpec {RoSpecId} (Start={Start}, Stop={Stop}).",
            _readerId, roSpec.RoSpecId, roSpec.StartTrigger.TriggerType, roSpec.StopTrigger.TriggerType);

        // Step 2: ADD_ROSPEC – tell the reader about the new ROSpec definition.
        await SendAddRoSpecAsync(roSpec, cancellationToken);

        // Step 3: ENABLE_ROSPEC – transition the ROSpec from Disabled to Inactive.
        await SendEnableRoSpecAsync(roSpec.RoSpecId, cancellationToken);

        // Step 4: START_ROSPEC – transition the ROSpec from Inactive to Active,
        //         which begins tag inventory (if the start trigger is Immediate).
        await SendStartRoSpecAsync(roSpec.RoSpecId, cancellationToken);

        _roSpecApplied = true;

        _logger.LogInformation(
            "[{ReaderId}] ROSpec {RoSpecId} applied and started.", _readerId, roSpec.RoSpecId);
    }

    /// <summary>
    /// Sends the STOP_ROSPEC and DISABLE_ROSPEC commands to the reader
    /// so the current inventory round is cleanly terminated before disconnection.
    /// </summary>
    private async Task TearDownRoSpecAsync()
    {
        if (!_roSpecApplied || _activeRoSpecId is null)
        {
            _logger.LogDebug("[{ReaderId}] No active ROSpec to tear down.", _readerId);
            return;
        }

        var roSpecId = _activeRoSpecId.Value;

        try
        {
            // Step 1: STOP_ROSPEC – halt the active inventory round.
            await SendStopRoSpecAsync(roSpecId);

            // Step 2: DISABLE_ROSPEC – move the ROSpec back to the Disabled state.
            await SendDisableRoSpecAsync(roSpecId);

            _logger.LogInformation(
                "[{ReaderId}] ROSpec {RoSpecId} stopped and disabled.", _readerId, roSpecId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{ReaderId}] Error tearing down ROSpec {RoSpecId} during shutdown.", _readerId, roSpecId);
        }
        finally
        {
            _roSpecApplied = false;
            _activeRoSpecId = null;
        }
    }

    // --------------------------------------------------------------
    // LLRP command stubs – placeholder implementations
    // Each method represents a single LLRP command message.
    // Binary LLRP encoding and response handling will be added later.
    // --------------------------------------------------------------

    /// <summary>
    /// Sends an ADD_ROSPEC command to register the given ROSpec on the reader.
    /// </summary>
    private async Task SendAddRoSpecAsync(RoSpec roSpec, CancellationToken cancellationToken)
    {
        _logger.LogDebug("[{ReaderId}] ? ADD_ROSPEC (ID={RoSpecId}).", _readerId, roSpec.RoSpecId);

        var message = LlrpMessageEncoder.BuildAddRoSpec(roSpec);
        await SendRawAsync(message, cancellationToken);

        var response = await ReadResponseAsync(LlrpMessageEncoder.MsgTypeAddRoSpecResponse, cancellationToken);
        ValidateResponse(response, "ADD_ROSPEC");
    }

    /// <summary>
    /// Sends an ENABLE_ROSPEC command to transition the ROSpec from
    /// Disabled to Inactive on the reader.
    /// </summary>
    private async Task SendEnableRoSpecAsync(uint roSpecId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("[{ReaderId}] ? ENABLE_ROSPEC (ID={RoSpecId}).", _readerId, roSpecId);

        var message = LlrpMessageEncoder.BuildEnableRoSpec(roSpecId);
        await SendRawAsync(message, cancellationToken);

        var response = await ReadResponseAsync(LlrpMessageEncoder.MsgTypeEnableRoSpecResponse, cancellationToken);
        ValidateResponse(response, "ENABLE_ROSPEC");
    }

    /// <summary>
    /// Sends a START_ROSPEC command to transition the ROSpec from
    /// Inactive to Active, beginning tag inventory.
    /// </summary>
    private async Task SendStartRoSpecAsync(uint roSpecId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("[{ReaderId}] ? START_ROSPEC (ID={RoSpecId}).", _readerId, roSpecId);

        var message = LlrpMessageEncoder.BuildStartRoSpec(roSpecId);
        await SendRawAsync(message, cancellationToken);

        var response = await ReadResponseAsync(LlrpMessageEncoder.MsgTypeStartRoSpecResponse, cancellationToken);
        ValidateResponse(response, "START_ROSPEC");
    }

    /// <summary>
    /// Sends a STOP_ROSPEC command to halt the active inventory round.
    /// </summary>
    private async Task SendStopRoSpecAsync(uint roSpecId)
    {
        _logger.LogDebug("[{ReaderId}] ? STOP_ROSPEC (ID={RoSpecId}).", _readerId, roSpecId);

        var message = LlrpMessageEncoder.BuildStopRoSpec(roSpecId);
        await SendRawAsync(message, CancellationToken.None);

        var response = await ReadResponseAsync(LlrpMessageEncoder.MsgTypeStopRoSpecResponse, CancellationToken.None);
        ValidateResponse(response, "STOP_ROSPEC");
    }

    /// <summary>
    /// Sends a DISABLE_ROSPEC command to move the ROSpec back to the
    /// Disabled state so it can be deleted or replaced.
    /// </summary>
    private async Task SendDisableRoSpecAsync(uint roSpecId)
    {
        _logger.LogDebug("[{ReaderId}] ? DISABLE_ROSPEC (ID={RoSpecId}).", _readerId, roSpecId);

        var message = LlrpMessageEncoder.BuildDisableRoSpec(roSpecId);
        await SendRawAsync(message, CancellationToken.None);

        var response = await ReadResponseAsync(LlrpMessageEncoder.MsgTypeDisableRoSpecResponse, CancellationToken.None);
        ValidateResponse(response, "DISABLE_ROSPEC");
    }

    // ---------------------------------------------------------------
    // Low-level send / receive helpers
    // ---------------------------------------------------------------

    /// <summary>
    /// Writes raw bytes to the TCP stream.
    /// </summary>
    private async Task SendRawAsync(byte[] data, CancellationToken cancellationToken)
    {
        if (_stream is null)
            throw new InvalidOperationException("Not connected to the reader.");

        await _stream.WriteAsync(data.AsMemory(), cancellationToken);
        await _stream.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Reads bytes from the TCP stream until a complete LLRP response of the
    /// expected type is received, or until <see cref="ResponseTimeout"/> elapses.
    /// </summary>
    private async Task<LlrpMessageEncoder.LlrpResponse> ReadResponseAsync(
        ushort expectedMessageType, CancellationToken cancellationToken)
    {
        if (_stream is null)
            throw new InvalidOperationException("Not connected to the reader.");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ResponseTimeout);
        var token = timeoutCts.Token;

        var responseBuffer = new MemoryStream();
        var readBuf = new byte[ReceiveBufferSize];

        while (true)
        {
            token.ThrowIfCancellationRequested();

            int bytesRead = await _stream.ReadAsync(readBuf.AsMemory(), token);
            if (bytesRead == 0)
                throw new IOException("Reader closed the connection while waiting for a response.");

            responseBuffer.Write(readBuf, 0, bytesRead);

            var accumulated = responseBuffer.GetBuffer().AsSpan(0, (int)responseBuffer.Length);

            if (accumulated.Length < LlrpMessageEncoder.HeaderSize)
                continue;

            var msgLength = BinaryPrimitives.ReadUInt32BigEndian(accumulated[2..]);
            if (accumulated.Length < (int)msgLength)
                continue;

            // We have a complete message.
            var message = accumulated[..(int)msgLength].ToArray();

            // Push any leftover bytes back into the framing buffer for the listen loop.
            var leftover = accumulated[(int)msgLength..];
            if (leftover.Length > 0)
                _frameBuffer.Write(leftover);

            if (LlrpMessageEncoder.TryParseResponse(message, out var response))
            {
                // If the reader sent a KEEPALIVE while we're waiting, ack it and keep reading.
                if (response.MessageType == LlrpMessageEncoder.MsgTypeKeepalive)
                {
                    _logger.LogDebug("[{ReaderId}] KEEPALIVE during response wait – sending ACK.", _readerId);
                    await SendRawAsync(LlrpMessageEncoder.BuildKeepaliveAck(), token);
                    responseBuffer.SetLength(0);
                    continue;
                }

                if (response.MessageType == expectedMessageType)
                    return response;

                // Unexpected message type – log and keep waiting.
                _logger.LogDebug(
                    "[{ReaderId}] Received unexpected message type {Type} while waiting for {Expected}.",
                    _readerId,
                    LlrpMessageEncoder.GetMessageTypeName(response.MessageType),
                    LlrpMessageEncoder.GetMessageTypeName(expectedMessageType));

                responseBuffer.SetLength(0);
                continue;
            }

            throw new InvalidOperationException(
                $"Failed to parse response for {LlrpMessageEncoder.GetMessageTypeName(expectedMessageType)}.");
        }
    }

    /// <summary>
    /// Validates that an LLRP response indicates success; throws on error.
    /// </summary>
    private void ValidateResponse(LlrpMessageEncoder.LlrpResponse response, string commandName)
    {
        if (response.StatusCode == LlrpMessageEncoder.LlrpStatusCode.Success)
        {
            _logger.LogDebug("[{ReaderId}] ? {Command}_RESPONSE: Success.", _readerId, commandName);
            return;
        }

        _logger.LogError(
            "[{ReaderId}] ? {Command}_RESPONSE: Error {Code} – {Desc}.",
            _readerId, commandName, response.StatusCode,
            response.ErrorDescription ?? "(no description)");

        throw new InvalidOperationException(
            $"LLRP {commandName} failed with status {response.StatusCode}: {response.ErrorDescription}");
    }

    // --------------------------------------------------------------

    // --------------------------------------------------------------
    // State management
    // --------------------------------------------------------------

    /// <summary>
    /// Transitions the reader to a new state, logs the change, and raises
    /// <see cref="StateChanged"/>. No-ops if the state has not changed.
    /// </summary>
    private void TransitionState(ReaderState newState)
    {
        var previousState = _state;
        if (previousState == newState)
            return;

        _state = newState;

        _logger.LogInformation(
            "[{ReaderId}] State transition: {Previous} ? {New}.",
            _readerId, previousState, newState);

        try
        {
            StateChanged?.Invoke(_readerId, previousState, newState);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{ReaderId}] StateChanged handler threw an exception.", _readerId);
        }
    }

    // --------------------------------------------------------------

    /// <summary>
    /// Tears down the TCP connection and releases network resources.
    /// Sends an LLRP CLOSE_CONNECTION message before closing the socket.
    /// </summary>
    private void Disconnect()
    {
        if (_stream is not null)
        {
            try
            {
                _logger.LogDebug("[{ReaderId}] Sending CLOSE_CONNECTION.", _readerId);
                var closeMsg = LlrpMessageEncoder.BuildCloseConnection();
                _stream.Write(closeMsg);
                _stream.Flush();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[{ReaderId}] Failed to send CLOSE_CONNECTION (stream may already be closed).", _readerId);
            }
        }

        _stream?.Dispose();
        _stream = null;

        _tcpClient?.Dispose();
        _tcpClient = null;

        _frameBuffer.SetLength(0);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _listenCts?.Cancel();
        _listenCts?.Dispose();
        Disconnect();

        // Ensure state reflects disposal even if StopAsync was not called.
        if (_state != ReaderState.Disconnected)
            TransitionState(ReaderState.Disconnected);

        _disposed = true;
    }
}
