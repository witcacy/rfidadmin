using System.Buffers.Binary;

namespace Rfid.IngestionService.Llrp;

/// <summary>
/// Encodes and decodes LLRP binary messages per the LLRP v1.1 specification.
/// <para>
/// Every LLRP message has a 10-byte header:
/// <list type="bullet">
///   <item>Bytes 0–1: [Version(3 bits) | MessageType(10 bits) | Reserved(3 bits)]</item>
///   <item>Bytes 2–5: MessageLength (uint32, big-endian; includes the 10-byte header)</item>
///   <item>Bytes 6–9: MessageID    (uint32, big-endian)</item>
/// </list>
/// </para>
/// </summary>
internal static class LlrpMessageEncoder
{
    // ---------------------------------------------------------------
    // LLRP protocol version
    // ---------------------------------------------------------------

    /// <summary>LLRP v1.1 version number (occupies bits 0–2 of byte 0).</summary>
    public const byte LlrpVersion = 1;

    // ---------------------------------------------------------------
    // LLRP message header size
    // ---------------------------------------------------------------

    /// <summary>All LLRP messages have a 10-byte header.</summary>
    public const int HeaderSize = 10;

    // ---------------------------------------------------------------
    // LLRP message type constants (10-bit values)
    // ---------------------------------------------------------------

    public const ushort MsgTypeCloseConnection          = 14;
    public const ushort MsgTypeCloseConnectionResponse  = 4;
    public const ushort MsgTypeAddRoSpec                = 20;
    public const ushort MsgTypeAddRoSpecResponse        = 30;
    public const ushort MsgTypeEnableRoSpec             = 24;
    public const ushort MsgTypeEnableRoSpecResponse     = 34;
    public const ushort MsgTypeStartRoSpec              = 22;
    public const ushort MsgTypeStartRoSpecResponse      = 32;
    public const ushort MsgTypeStopRoSpec               = 23;
    public const ushort MsgTypeStopRoSpecResponse       = 33;
    public const ushort MsgTypeDisableRoSpec            = 25;
    public const ushort MsgTypeDisableRoSpecResponse    = 35;
    public const ushort MsgTypeDeleteRoSpec             = 21;
    public const ushort MsgTypeDeleteRoSpecResponse     = 31;
    public const ushort MsgTypeKeepalive                = 62;
    public const ushort MsgTypeKeepaliveAck             = 72;
    public const ushort MsgTypeReaderEventNotification  = 63;
    public const ushort MsgTypeErrorMessage             = 100;
    public const ushort MsgTypeRoAccessReport           = 61;

    // ---------------------------------------------------------------
    // LLRP TLV parameter type constants
    // ---------------------------------------------------------------

    private const ushort ParamTypeRoSpec                    = 177;
    private const ushort ParamTypeRoSpecStartTrigger        = 179;
    private const ushort ParamTypeRoSpecStopTrigger         = 182;
    private const ushort ParamTypeAiSpec                    = 183;
    private const ushort ParamTypeAiSpecStopTrigger         = 184;
    private const ushort ParamTypeInventoryParameterSpec    = 186;
    private const ushort ParamTypeAntennaConfiguration      = 222;
    private const ushort ParamTypeRoReportSpec              = 237;
    private const ushort ParamTypeTagReportContentSelector  = 238;
    private const ushort ParamTypeRoBoundarySpec            = 178;
    private const ushort ParamTypeLlrpStatus                = 287;
    private const ushort ParamTypePeriodicTriggerValue      = 180;
    private const ushort ParamTypeGpiTriggerValue           = 181;
    private const ushort ParamTypeTagReportData             = 240;
    private const ushort ParamTypeEpcData                   = 241;

    // TV parameter types (bit 15 set in the header indicates TV encoding)
    private const ushort TvParamTypeAntennaId                   = 1;
    private const ushort TvParamTypeFirstSeenTimestampUtc        = 2;
    private const ushort TvParamTypeLastSeenTimestampUtc         = 3;
    private const ushort TvParamTypePeakRssi                    = 6;
    private const ushort TvParamTypeChannelIndex                 = 7;
    private const ushort TvParamTypeTagSeenCount                 = 8;
    private const ushort TvParamTypeInventoryParameterSpecId     = 10;
    private const ushort TvParamTypeAccessSpecId                 = 16;

    /// <summary>LLRP epoch: 2006-01-01T00:00:00Z.</summary>
    private static readonly DateTimeOffset LlrpEpoch = new(2006, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------------
    // Auto-incrementing message ID
    // ---------------------------------------------------------------

    private static uint _nextMessageId;

    private static uint NextMessageId() => Interlocked.Increment(ref _nextMessageId);

    // ===============================================================
    // Header helpers
    // ===============================================================

    /// <summary>
    /// Tries to parse the 10-byte LLRP header from the front of <paramref name="data"/>.
    /// </summary>
    public static bool TryParseHeader(
        ReadOnlySpan<byte> data,
        out byte version,
        out ushort messageType,
        out uint messageLength,
        out uint messageId)
    {
        version = 0;
        messageType = 0;
        messageLength = 0;
        messageId = 0;

        if (data.Length < HeaderSize)
            return false;

        // Byte 0–1: [VVV T TTTT TTTT TRRR]
        version = (byte)((data[0] >> 2) & 0x07);
        messageType = (ushort)(((data[0] & 0x03) << 8) | data[1]);
        messageLength = BinaryPrimitives.ReadUInt32BigEndian(data[2..]);
        messageId = BinaryPrimitives.ReadUInt32BigEndian(data[6..]);

        return true;
    }

    /// <summary>
    /// Returns the human-readable name of a known LLRP message type.
    /// </summary>
    public static string GetMessageTypeName(ushort messageType) => messageType switch
    {
        MsgTypeCloseConnection         => "CLOSE_CONNECTION",
        MsgTypeCloseConnectionResponse => "CLOSE_CONNECTION_RESPONSE",
        MsgTypeAddRoSpec               => "ADD_ROSPEC",
        MsgTypeAddRoSpecResponse       => "ADD_ROSPEC_RESPONSE",
        MsgTypeEnableRoSpec            => "ENABLE_ROSPEC",
        MsgTypeEnableRoSpecResponse    => "ENABLE_ROSPEC_RESPONSE",
        MsgTypeStartRoSpec             => "START_ROSPEC",
        MsgTypeStartRoSpecResponse     => "START_ROSPEC_RESPONSE",
        MsgTypeStopRoSpec              => "STOP_ROSPEC",
        MsgTypeStopRoSpecResponse      => "STOP_ROSPEC_RESPONSE",
        MsgTypeDisableRoSpec           => "DISABLE_ROSPEC",
        MsgTypeDisableRoSpecResponse   => "DISABLE_ROSPEC_RESPONSE",
        MsgTypeDeleteRoSpec            => "DELETE_ROSPEC",
        MsgTypeDeleteRoSpecResponse    => "DELETE_ROSPEC_RESPONSE",
        MsgTypeKeepalive               => "KEEPALIVE",
        MsgTypeKeepaliveAck            => "KEEPALIVE_ACK",
        MsgTypeReaderEventNotification => "READER_EVENT_NOTIFICATION",
        MsgTypeErrorMessage            => "ERROR_MESSAGE",
        MsgTypeRoAccessReport          => "RO_ACCESS_REPORT",
        _ => $"UNKNOWN({messageType})"
    };

    // ===============================================================
    // Encoding: complete LLRP messages
    // ===============================================================

    /// <summary>
    /// Builds a complete LLRP message frame (header + body).
    /// </summary>
    private static byte[] BuildMessage(ushort messageType, ReadOnlySpan<byte> body)
    {
        var totalLength = (uint)(HeaderSize + body.Length);
        var buffer = new byte[totalLength];
        var messageId = NextMessageId();

        // Header byte 0–1: [VVV T TTTT TTTT TRRR]
        buffer[0] = (byte)((LlrpVersion << 2) | ((messageType >> 8) & 0x03));
        buffer[1] = (byte)(messageType & 0xFF);

        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(2), totalLength);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(6), messageId);

        body.CopyTo(buffer.AsSpan(HeaderSize));

        return buffer;
    }

    /// <summary>
    /// Builds a simple LLRP command that contains only a ROSpec ID in the body
    /// (ENABLE_ROSPEC, START_ROSPEC, STOP_ROSPEC, DISABLE_ROSPEC, DELETE_ROSPEC).
    /// </summary>
    private static byte[] BuildRoSpecIdCommand(ushort messageType, uint roSpecId)
    {
        Span<byte> body = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(body, roSpecId);
        return BuildMessage(messageType, body);
    }

    /// <summary>Builds an ENABLE_ROSPEC message.</summary>
    public static byte[] BuildEnableRoSpec(uint roSpecId)
        => BuildRoSpecIdCommand(MsgTypeEnableRoSpec, roSpecId);

    /// <summary>Builds a START_ROSPEC message.</summary>
    public static byte[] BuildStartRoSpec(uint roSpecId)
        => BuildRoSpecIdCommand(MsgTypeStartRoSpec, roSpecId);

    /// <summary>Builds a STOP_ROSPEC message.</summary>
    public static byte[] BuildStopRoSpec(uint roSpecId)
        => BuildRoSpecIdCommand(MsgTypeStopRoSpec, roSpecId);

    /// <summary>Builds a DISABLE_ROSPEC message.</summary>
    public static byte[] BuildDisableRoSpec(uint roSpecId)
        => BuildRoSpecIdCommand(MsgTypeDisableRoSpec, roSpecId);

    /// <summary>Builds a DELETE_ROSPEC message (roSpecId 0 = delete all).</summary>
    public static byte[] BuildDeleteRoSpec(uint roSpecId)
        => BuildRoSpecIdCommand(MsgTypeDeleteRoSpec, roSpecId);

    /// <summary>Builds a CLOSE_CONNECTION message (empty body).</summary>
    public static byte[] BuildCloseConnection()
        => BuildMessage(MsgTypeCloseConnection, ReadOnlySpan<byte>.Empty);

    /// <summary>Builds a KEEPALIVE_ACK message (empty body).</summary>
    public static byte[] BuildKeepaliveAck()
        => BuildMessage(MsgTypeKeepaliveAck, ReadOnlySpan<byte>.Empty);

    // ---------------------------------------------------------------
    // ADD_ROSPEC encoding
    // ---------------------------------------------------------------

    /// <summary>
    /// Builds an ADD_ROSPEC message containing the full ROSpec definition.
    /// </summary>
    public static byte[] BuildAddRoSpec(RoSpec roSpec)
    {
        var roSpecParam = EncodeRoSpecParameter(roSpec);
        return BuildMessage(MsgTypeAddRoSpec, roSpecParam);
    }

    /// <summary>
    /// Encodes a ROSpec as a TLV parameter (type 177).
    /// Layout: ROSpecID(4) + Priority(1) + CurrentState(1=Disabled) + ROBoundarySpec + AISpec + ROReportSpec
    /// </summary>
    private static byte[] EncodeRoSpecParameter(RoSpec roSpec)
    {
        using var ms = new MemoryStream();

        // Fixed fields: ROSpecID (4) + Priority (1) + CurrentState (1, always Disabled=0 for ADD)
        WriteUInt32BigEndian(ms, roSpec.RoSpecId);
        ms.WriteByte(roSpec.Priority);
        ms.WriteByte(0); // CurrentState = Disabled

        // ROBoundarySpec (TLV type 178): contains StartTrigger + StopTrigger
        var boundarySpec = EncodeRoBoundarySpec(roSpec.StartTrigger, roSpec.StopTrigger);
        ms.Write(boundarySpec);

        // AISpec (TLV type 183)
        var aiSpec = EncodeAiSpec(roSpec.AntennaSpec);
        ms.Write(aiSpec);

        // ROReportSpec (TLV type 237)
        var reportSpec = EncodeRoReportSpec(roSpec.ReportTrigger, roSpec.ReportContentFlags);
        ms.Write(reportSpec);

        var body = ms.ToArray();
        return WrapTlv(ParamTypeRoSpec, body);
    }

    /// <summary>
    /// ROBoundarySpec (type 178) = ROSpecStartTrigger + ROSpecStopTrigger.
    /// </summary>
    private static byte[] EncodeRoBoundarySpec(RoSpecStartTrigger start, RoSpecStopTrigger stop)
    {
        using var ms = new MemoryStream();

        // ROSpecStartTrigger (type 179)
        ms.Write(EncodeStartTrigger(start));

        // ROSpecStopTrigger (type 182)
        ms.Write(EncodeStopTrigger(stop));

        return WrapTlv(ParamTypeRoBoundarySpec, ms.ToArray());
    }

    private static byte[] EncodeStartTrigger(RoSpecStartTrigger trigger)
    {
        using var ms = new MemoryStream();

        // ROSpecStartTriggerType (1 byte)
        ms.WriteByte((byte)trigger.TriggerType);

        // Sub-parameter depending on trigger type
        if (trigger.TriggerType == RoSpecStartTriggerType.Periodic)
        {
            // PeriodicTriggerValue (TLV type 180): Offset(4) + Period(4)
            using var sub = new MemoryStream();
            WriteUInt32BigEndian(sub, 0); // Offset = 0
            WriteUInt32BigEndian(sub, trigger.PeriodicTriggerValueMs);
            ms.Write(WrapTlv(ParamTypePeriodicTriggerValue, sub.ToArray()));
        }
        else if (trigger.TriggerType == RoSpecStartTriggerType.GpiTrigger)
        {
            // GPITriggerValue (TLV type 181): GPIPortNum(2) + GPIEvent(1) + Timeout(4)
            using var sub = new MemoryStream();
            WriteUInt16BigEndian(sub, trigger.GpiPortNumber);
            sub.WriteByte(trigger.GpiTriggerOnHigh ? (byte)1 : (byte)0);
            WriteUInt32BigEndian(sub, 0); // Timeout = 0 (no timeout)
            ms.Write(WrapTlv(ParamTypeGpiTriggerValue, sub.ToArray()));
        }

        return WrapTlv(ParamTypeRoSpecStartTrigger, ms.ToArray());
    }

    private static byte[] EncodeStopTrigger(RoSpecStopTrigger trigger)
    {
        using var ms = new MemoryStream();

        // ROSpecStopTriggerType (1 byte)
        ms.WriteByte((byte)trigger.TriggerType);

        // DurationTriggerValue (4 bytes) — present regardless of type; 0 if not Duration.
        WriteUInt32BigEndian(ms, trigger.TriggerType == RoSpecStopTriggerType.Duration
            ? trigger.DurationMs
            : 0);

        return WrapTlv(ParamTypeRoSpecStopTrigger, ms.ToArray());
    }

    /// <summary>
    /// AISpec (type 183) = AntennaCount(2) + AntennaIDs(2 each) + AISpecStopTrigger + InventoryParameterSpec
    /// </summary>
    private static byte[] EncodeAiSpec(AntennaInventorySpec spec)
    {
        using var ms = new MemoryStream();

        // If no antennas specified, use antenna 0 to mean "all".
        var antennas = spec.AntennaIds.Count > 0 ? spec.AntennaIds : [0];

        // AntennaCount (2 bytes)
        WriteUInt16BigEndian(ms, (ushort)antennas.Count);

        // AntennaIDs
        foreach (var antennaId in antennas)
        {
            WriteUInt16BigEndian(ms, antennaId);
        }

        // AISpecStopTrigger (type 184): TriggerType(1) + DurationTrigger(4)
        // Type 0 = Null (no stop trigger at AISpec level; ROSpec controls it).
        using var stopTrigger = new MemoryStream();
        stopTrigger.WriteByte(0); // Null trigger
        WriteUInt32BigEndian(stopTrigger, 0);
        ms.Write(WrapTlv(ParamTypeAiSpecStopTrigger, stopTrigger.ToArray()));

        // InventoryParameterSpec (type 186): InventoryParameterSpecID(2) + ProtocolID(1)
        using var invSpec = new MemoryStream();
        WriteUInt16BigEndian(invSpec, 1); // InventoryParameterSpecID = 1
        invSpec.WriteByte(1);             // ProtocolID = 1 (EPCGlobalClass1Gen2)
        ms.Write(WrapTlv(ParamTypeInventoryParameterSpec, invSpec.ToArray()));

        return WrapTlv(ParamTypeAiSpec, ms.ToArray());
    }

    /// <summary>
    /// ROReportSpec (type 237) = ROReportTrigger(1) + N(2) + TagReportContentSelector
    /// </summary>
    private static byte[] EncodeRoReportSpec(RoReportTrigger trigger, TagReportContentFlags flags)
    {
        using var ms = new MemoryStream();

        // ROReportTrigger (1 byte)
        ms.WriteByte((byte)trigger);

        // N – tag count threshold for UponNTags; 0 otherwise.
        WriteUInt16BigEndian(ms, 0);

        // TagReportContentSelector (type 238)
        ms.Write(EncodeTagReportContentSelector(flags));

        return WrapTlv(ParamTypeRoReportSpec, ms.ToArray());
    }

    /// <summary>
    /// TagReportContentSelector (type 238).
    /// Encodes a 2-byte bit-field that controls which optional TV parameters
    /// the reader includes in each TagReportData entry.
    /// </summary>
    private static byte[] EncodeTagReportContentSelector(TagReportContentFlags flags)
    {
        // The LLRP spec defines these as individual bit positions within a
        // two-byte "EnableROSpecID + Enable..." set of flags.
        ushort bits = 0;

        // Bit layout (LLRP v1.1 §14.2.1.1):
        //   Bit 15: EnableROSpecID          (always 1)
        //   Bit 14: EnableSpecIndex         (always 1)
        //   Bit 13: EnableInventoryParameterSpecID
        //   Bit 12: EnableAntennaID
        //   Bit 11: EnableChannelIndex
        //   Bit 10: EnablePeakRSSI
        //   Bit  9: EnableFirstSeenTimestamp
        //   Bit  8: EnableLastSeenTimestamp
        //   Bit  7: EnableTagSeenCount
        //   Bit  6: EnableAccessSpecID
        bits |= (1 << 15); // ROSpecID always on
        bits |= (1 << 14); // SpecIndex always on

        if (flags.HasFlag(TagReportContentFlags.AntennaId))         bits |= (1 << 12);
        if (flags.HasFlag(TagReportContentFlags.ChannelIndex))      bits |= (1 << 11);
        if (flags.HasFlag(TagReportContentFlags.PeakRssi))          bits |= (1 << 10);
        if (flags.HasFlag(TagReportContentFlags.FirstSeenTimestamp)) bits |= (1 << 9);
        if (flags.HasFlag(TagReportContentFlags.LastSeenTimestamp))  bits |= (1 << 8);
        if (flags.HasFlag(TagReportContentFlags.TagSeenCount))      bits |= (1 << 7);
        if (flags.HasFlag(TagReportContentFlags.AccessSpecId))      bits |= (1 << 6);

        Span<byte> body = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(body, bits);
        return WrapTlv(ParamTypeTagReportContentSelector, body);
    }

    // ===============================================================
    // Response parsing
    // ===============================================================

    /// <summary>
    /// Represents the result of parsing an LLRP response message.
    /// </summary>
    public readonly record struct LlrpResponse(ushort MessageType, uint MessageId, LlrpStatusCode StatusCode, string? ErrorDescription);

    /// <summary>LLRP status codes from the LLRPStatus parameter.</summary>
    public enum LlrpStatusCode : ushort
    {
        Success = 0,
        ParameterError = 100,
        FieldError = 101,
        UnexpectedParameter = 102,
        MissingParameter = 103,
        DuplicateParameter = 104,
        OverflowParameter = 105,
        OverflowField = 106,
        UnknownParameter = 107,
        UnknownField = 108,
        UnsupportedMessage = 109,
        UnsupportedVersion = 110,
        UnsupportedParameter = 111,
        // Device-specific errors >= 200
    }

    /// <summary>
    /// Parses an LLRP response message and extracts the LLRPStatus parameter.
    /// </summary>
    public static bool TryParseResponse(ReadOnlySpan<byte> data, out LlrpResponse response)
    {
        response = default;

        if (!TryParseHeader(data, out _, out var msgType, out var msgLength, out var msgId))
            return false;

        var bodyLength = (int)Math.Min(msgLength, (uint)data.Length) - HeaderSize;
        if (bodyLength < 0)
            return false;

        var body = data.Slice(HeaderSize, bodyLength);

        // Search for the LLRPStatus TLV parameter (type 287).
        var statusCode = LlrpStatusCode.Success;
        string? errorDesc = null;

        var offset = 0;
        while (offset + 4 <= body.Length)
        {
            var paramHeader = BinaryPrimitives.ReadUInt16BigEndian(body[offset..]);

            // Skip TV parameters
            if ((paramHeader & 0x8000) != 0)
            {
                offset += GetTvParameterLength((ushort)(paramHeader & 0x7FFF));
                continue;
            }

            var tlvType = paramHeader;
            var tlvLength = BinaryPrimitives.ReadUInt16BigEndian(body[(offset + 2)..]);

            if (tlvLength < 4 || offset + tlvLength > body.Length)
                break;

            if (tlvType == ParamTypeLlrpStatus && tlvLength >= 8)
            {
                // LLRPStatus layout: StatusCode(2) + ErrorDescriptionByteCount(2) + ErrorDescription(variable)
                statusCode = (LlrpStatusCode)BinaryPrimitives.ReadUInt16BigEndian(body[(offset + 4)..]);
                var descByteCount = BinaryPrimitives.ReadUInt16BigEndian(body[(offset + 6)..]);
                if (descByteCount > 0 && offset + 8 + descByteCount <= body.Length)
                {
                    errorDesc = System.Text.Encoding.UTF8.GetString(
                        body.Slice(offset + 8, descByteCount));
                }
            }

            offset += tlvLength;
        }

        response = new LlrpResponse(msgType, msgId, statusCode, errorDesc);
        return true;
    }

    // ===============================================================
    // TagReportData decoding
    // ===============================================================

    /// <summary>
    /// Represents a single tag read extracted from an RO_ACCESS_REPORT.
    /// </summary>
    public sealed class TagReadResult
    {
        public string Epc { get; init; } = string.Empty;
        public int AntennaPort { get; init; }
        public DateTimeOffset TimestampUtc { get; init; }
        public double? Rssi { get; init; }
        public ushort? ChannelIndex { get; init; }
        public ushort? TagSeenCount { get; init; }
        public uint? AccessSpecId { get; init; }
    }

    /// <summary>
    /// Extracts all TagReportData entries from a complete RO_ACCESS_REPORT message buffer.
    /// </summary>
    public static List<TagReadResult> ExtractTagReads(ReadOnlySpan<byte> message)
    {
        var results = new List<TagReadResult>();

        if (!TryParseHeader(message, out _, out var msgType, out var msgLength, out _))
            return results;

        if (msgType != MsgTypeRoAccessReport)
            return results;

        var bodyLength = (int)Math.Min(msgLength, (uint)message.Length) - HeaderSize;
        if (bodyLength <= 0)
            return results;

        var body = message.Slice(HeaderSize, bodyLength);
        var offset = 0;

        while (offset + 4 <= body.Length)
        {
            var paramHeader = BinaryPrimitives.ReadUInt16BigEndian(body[offset..]);

            // Skip TV parameters at message level
            if ((paramHeader & 0x8000) != 0)
            {
                offset += GetTvParameterLength((ushort)(paramHeader & 0x7FFF));
                continue;
            }

            var tlvType = paramHeader;
            var tlvLength = BinaryPrimitives.ReadUInt16BigEndian(body[(offset + 2)..]);

            if (tlvLength < 4 || offset + tlvLength > body.Length)
                break;

            if (tlvType == ParamTypeTagReportData)
            {
                var tagBody = body.Slice(offset + 4, tlvLength - 4);
                var tag = ParseSingleTagReportData(tagBody);
                if (tag is not null)
                    results.Add(tag);
            }

            offset += tlvLength;
        }

        return results;
    }

    /// <summary>
    /// Parses a single TagReportData TLV body into a <see cref="TagReadResult"/>.
    /// </summary>
    private static TagReadResult? ParseSingleTagReportData(ReadOnlySpan<byte> data)
    {
        string epc = string.Empty;
        int antennaPort = 0;
        DateTimeOffset? timestampUtc = null;
        double? rssi = null;
        ushort? channelIndex = null;
        ushort? tagSeenCount = null;
        uint? accessSpecId = null;

        var offset = 0;
        while (offset < data.Length)
        {
            if (offset + 2 > data.Length)
                break;

            var header = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);

            // ---- TV-encoded parameters (bit 15 set) ----
            if ((header & 0x8000) != 0)
            {
                var tvType = (ushort)(header & 0x7FFF);
                var tvLen = GetTvParameterLength(tvType);

                if (offset + tvLen > data.Length)
                    break;

                switch (tvType)
                {
                    case TvParamTypeAntennaId when offset + 4 <= data.Length:
                        antennaPort = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]);
                        break;

                    case TvParamTypeFirstSeenTimestampUtc when offset + 10 <= data.Length:
                        var micros = BinaryPrimitives.ReadUInt64BigEndian(data[(offset + 2)..]);
                        if (micros > 0)
                            timestampUtc = LlrpEpoch.AddTicks((long)(micros * 10));
                        break;

                    case TvParamTypePeakRssi when offset + 3 <= data.Length:
                        rssi = (sbyte)data[offset + 2];
                        break;

                    case TvParamTypeChannelIndex when offset + 4 <= data.Length:
                        channelIndex = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]);
                        break;

                    case TvParamTypeTagSeenCount when offset + 4 <= data.Length:
                        tagSeenCount = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]);
                        break;

                    case TvParamTypeAccessSpecId when offset + 6 <= data.Length:
                        accessSpecId = BinaryPrimitives.ReadUInt32BigEndian(data[(offset + 2)..]);
                        break;
                }

                offset += tvLen;
                continue;
            }

            // ---- TLV-encoded parameters (bit 15 clear) ----
            if (offset + 4 > data.Length)
                break;

            var tlvType = header;
            var tlvLength = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]);

            if (tlvLength < 4 || offset + tlvLength > data.Length)
                break;

            if (tlvType == ParamTypeEpcData && tlvLength > 6)
            {
                var epcBitCount = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 4)..]);
                var epcByteCount = Math.Min((epcBitCount + 7) / 8, tlvLength - 6);
                epc = Convert.ToHexString(data.Slice(offset + 6, epcByteCount)).ToLowerInvariant();
            }

            offset += tlvLength;
        }

        if (string.IsNullOrEmpty(epc))
            return null;

        return new TagReadResult
        {
            Epc = epc,
            AntennaPort = antennaPort,
            TimestampUtc = timestampUtc ?? DateTimeOffset.UtcNow,
            Rssi = rssi,
            ChannelIndex = channelIndex,
            TagSeenCount = tagSeenCount,
            AccessSpecId = accessSpecId,
        };
    }

    // ===============================================================
    // Shared TV parameter length table
    // ===============================================================

    /// <summary>
    /// Returns the total byte length of a TV-encoded parameter given its type.
    /// </summary>
    internal static int GetTvParameterLength(ushort tvType) => tvType switch
    {
        TvParamTypeAntennaId               => 4,   // 2 header + 2 value
        TvParamTypeFirstSeenTimestampUtc    => 10,  // 2 header + 8 value
        TvParamTypeLastSeenTimestampUtc     => 10,
        TvParamTypePeakRssi                 => 3,   // 2 header + 1 value
        TvParamTypeChannelIndex             => 4,
        TvParamTypeTagSeenCount             => 4,
        TvParamTypeInventoryParameterSpecId => 4,
        TvParamTypeAccessSpecId             => 6,   // 2 header + 4 value
        _ => 2  // Unknown TV – skip header only (best effort).
    };

    // ===============================================================
    // TLV / binary helpers
    // ===============================================================

    /// <summary>Wraps <paramref name="body"/> in a TLV envelope with the given type.</summary>
    private static byte[] WrapTlv(ushort paramType, ReadOnlySpan<byte> body)
    {
        var totalLength = 4 + body.Length; // 2 type + 2 length + body
        var buffer = new byte[totalLength];
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0), paramType);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2), (ushort)totalLength);
        body.CopyTo(buffer.AsSpan(4));
        return buffer;
    }

    private static void WriteUInt16BigEndian(Stream stream, ushort value)
    {
        Span<byte> buf = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buf, value);
        stream.Write(buf);
    }

    private static void WriteUInt32BigEndian(Stream stream, uint value)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buf, value);
        stream.Write(buf);
    }
}
