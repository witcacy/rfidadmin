using System.Buffers.Binary;
using Rfid.IngestionService.Models;

namespace Rfid.IngestionService.Normalization;

/// <summary>
/// Maps raw LLRP reader data into the canonical <see cref="RfidEvent"/> model.
/// <para>
/// The <see cref="Map"/> method accepts a raw <c>byte[]</c> payload from the
/// LLRP TCP stream, parses the 10-byte LLRP message header, and when the
/// message is an RO_ACCESS_REPORT (type 61) it walks the TLV parameters to
/// extract each TagReportData entry.  For every other message type the
/// method returns <c>null</c>.
/// </para>
/// <para>
/// <see cref="MapFromLlrpTagReport"/> accepts pre-extracted fields (used by
/// <see cref="Llrp.LlrpClient"/>) and maps them into the canonical model
/// including optional LLRP fields such as ChannelIndex, TagSeenCount,
/// AccessSpecID, InventoryParameterSpecID and OpSpecResult.
/// </para>
/// </summary>
public class RfidEventMapper
{
    // -- LLRP message / parameter type constants --
    private const ushort MsgTypeRoAccessReport = 61;

    // TLV parameter types (from LLRP v1.1 spec, Table 16)
    private const ushort ParamTypeTagReportData       = 240;
    private const ushort ParamTypeEpcData              = 241;

    // TV (type-value) parameter types (bit 15 set = TV encoding)
    private const ushort TvParamTypeAntennaId                   = 1;
    private const ushort TvParamTypeFirstSeenTimestampUtc        = 2;
    private const ushort TvParamTypeLastSeenTimestampUtc         = 3;
    private const ushort TvParamTypePeakRssi                    = 6;
    private const ushort TvParamTypeChannelIndex                 = 7;
    private const ushort TvParamTypeTagSeenCount                 = 8;
    private const ushort TvParamTypeInventoryParameterSpecId     = 10;
    private const ushort TvParamTypeAccessSpecId                 = 16;
    private const ushort TvParamTypeOpSpecResultStatus           = 17; // first field of C1G2ReadOpSpecResult etc.

    /// <summary>LLRP epoch: 2006-01-01T00:00:00Z in microseconds.</summary>
    private static readonly DateTimeOffset LlrpEpoch = new(2006, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ILogger<RfidEventMapper> _logger;

    public RfidEventMapper(ILogger<RfidEventMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ==============================================================
    // Map  (raw byte[] -> RfidEvent)
    // ==============================================================

    /// <summary>
    /// Parses a raw LLRP message.  If the message is an RO_ACCESS_REPORT
    /// the first TagReportData entry is extracted and mapped to an
    /// <see cref="RfidEvent"/>.  Returns <c>null</c> for any other message
    /// type or when the payload cannot be decoded.
    /// </summary>
    public RfidEvent? Map(string readerId, byte[] rawData)
    {
        if (rawData is null || rawData.Length == 0)
        {
            _logger.LogWarning("[{ReaderId}] Received empty payload -- skipping.", readerId);
            return null;
        }

        _logger.LogDebug("[{ReaderId}] Mapping {Bytes} bytes to RfidEvent.", readerId, rawData.Length);

        // -- Parse the 10-byte LLRP message header --
        if (!TryParseLlrpHeader(rawData, out var messageType, out var messageLength))
        {
            _logger.LogWarning("[{ReaderId}] Buffer too short for LLRP header ({Len} bytes).", readerId, rawData.Length);
            return null;
        }

        if (messageType != MsgTypeRoAccessReport)
        {
            _logger.LogDebug("[{ReaderId}] Ignoring LLRP message type {Type} (not RO_ACCESS_REPORT).", readerId, messageType);
            return null;
        }

        // -- Walk the message body and find the first TagReportData --
        var bodySpan = rawData.AsSpan(10, Math.Min((int)messageLength - 10, rawData.Length - 10));
        if (!TryExtractFirstTagReport(bodySpan, out var epc, out var antennaPort, out var timestampUtc,
                out var rssi, out var channelIndex, out var tagSeenCount,
                out var accessSpecId, out var inventoryParamSpecId, out var opSpecResult))
        {
            _logger.LogDebug("[{ReaderId}] No TagReportData found in RO_ACCESS_REPORT.", readerId);
            return null;
        }

        return new RfidEvent
        {
            Epc = epc,
            ReaderId = readerId,
            AntennaPort = antennaPort,
            Rssi = rssi ?? 0.0,
            Timestamp = timestampUtc ?? DateTimeOffset.UtcNow,
            SourceProtocol = "LLRP",
            ChannelIndex = channelIndex,
            TagSeenCount = tagSeenCount,
            AccessSpecId = accessSpecId,
            InventoryParameterSpecId = inventoryParamSpecId,
            OpSpecResultStatus = opSpecResult
        };
    }

    // ==============================================================
    // MapFromLlrpTagReport  (pre-extracted fields -> RfidEvent)
    // ==============================================================

    /// <summary>
    /// Maps pre-extracted LLRP TagReport fields into the canonical
    /// <see cref="RfidEvent"/> model, including optional LLRP parameters.
    /// </summary>
    public RfidEvent? MapFromLlrpTagReport(
        string readerId,
        string epc,
        int antennaPort,
        DateTimeOffset timestampUtc,
        double? rssi,
        ushort? channelIndex = null,
        ushort? tagSeenCount = null,
        uint? accessSpecId = null,
        ushort? inventoryParameterSpecId = null,
        ushort? opSpecResultStatus = null)
    {
        if (string.IsNullOrWhiteSpace(epc))
        {
            _logger.LogWarning("[{ReaderId}] TagReport with empty EPC -- skipping.", readerId);
            return null;
        }

        _logger.LogDebug(
            "[{ReaderId}] Mapping LLRP TagReport -- EPC: {Epc}, Antenna: {Antenna}, RSSI: {Rssi}, "
            + "Channel: {Channel}, SeenCount: {SeenCount}, AccessSpec: {AccessSpec}, "
            + "InvParamSpec: {InvParam}, OpSpecResult: {OpSpec}.",
            readerId, epc, antennaPort, rssi?.ToString("F1") ?? "N/A",
            channelIndex?.ToString() ?? "N/A",
            tagSeenCount?.ToString() ?? "N/A",
            accessSpecId?.ToString() ?? "N/A",
            inventoryParameterSpecId?.ToString() ?? "N/A",
            opSpecResultStatus?.ToString() ?? "N/A");

        return new RfidEvent
        {
            Epc = epc,
            ReaderId = readerId,
            AntennaPort = antennaPort,
            Rssi = rssi ?? 0.0,
            Timestamp = timestampUtc,
            SourceProtocol = "LLRP",
            ChannelIndex = channelIndex,
            TagSeenCount = tagSeenCount,
            AccessSpecId = accessSpecId,
            InventoryParameterSpecId = inventoryParameterSpecId,
            OpSpecResultStatus = opSpecResultStatus
        };
    }

    // ==============================================================
    // LLRP binary parsing helpers
    // ==============================================================

    /// <summary>
    /// Parses the 10-byte LLRP message header.
    /// Layout: [Version(3) | Type(10) | Reserved(3)] [MessageLength(32)] [MessageID(32)]
    /// </summary>
    private static bool TryParseLlrpHeader(ReadOnlySpan<byte> data, out ushort messageType, out uint messageLength)
    {
        messageType = 0;
        messageLength = 0;

        if (data.Length < 10)
            return false;

        // Bits 0-2 = version, bits 3-12 = message type (10 bits), bits 13-15 = reserved.
        // First two bytes: [VVV T TTTT] [TTTT TRRR]
        messageType = (ushort)(((data[0] & 0x03) << 8) | data[1]);
        messageLength = BinaryPrimitives.ReadUInt32BigEndian(data[2..]);

        return true;
    }

    /// <summary>
    /// Walks the message body looking for the first TagReportData (TLV type 240)
    /// parameter and extracts its sub-parameters.
    /// </summary>
    private bool TryExtractFirstTagReport(
        ReadOnlySpan<byte> body,
        out string epc,
        out int antennaPort,
        out DateTimeOffset? timestampUtc,
        out double? rssi,
        out ushort? channelIndex,
        out ushort? tagSeenCount,
        out uint? accessSpecId,
        out ushort? inventoryParamSpecId,
        out ushort? opSpecResult)
    {
        epc = string.Empty;
        antennaPort = 0;
        timestampUtc = null;
        rssi = null;
        channelIndex = null;
        tagSeenCount = null;
        accessSpecId = null;
        inventoryParamSpecId = null;
        opSpecResult = null;

        var offset = 0;
        while (offset + 4 <= body.Length)
        {
            var paramHeader = BinaryPrimitives.ReadUInt16BigEndian(body[offset..]);

            // TV-encoded parameters have bit 15 set.
            if ((paramHeader & 0x8000) != 0)
            {
                // TV parameter -- skip (these can appear at the message level too).
                var tvType = (ushort)(paramHeader & 0x7FFF);
                var tvLen = GetTvParameterLength(tvType);
                offset += tvLen;
                continue;
            }

            // TLV parameter: 2 bytes type + 2 bytes length (length includes the 4-byte header).
            if (offset + 4 > body.Length)
                break;

            var tlvType = paramHeader;
            var tlvLength = BinaryPrimitives.ReadUInt16BigEndian(body[(offset + 2)..]);

            if (tlvLength < 4 || offset + tlvLength > body.Length)
                break;

            if (tlvType == ParamTypeTagReportData)
            {
                // Parse sub-parameters within this TagReportData.
                var tagBody = body.Slice(offset + 4, tlvLength - 4);
                ParseTagReportData(tagBody, ref epc, ref antennaPort, ref timestampUtc,
                    ref rssi, ref channelIndex, ref tagSeenCount,
                    ref accessSpecId, ref inventoryParamSpecId, ref opSpecResult);
                return !string.IsNullOrEmpty(epc);
            }

            offset += tlvLength;
        }

        return false;
    }

    /// <summary>
    /// Parses the sub-parameters within a single TagReportData entry.
    /// </summary>
    private void ParseTagReportData(
        ReadOnlySpan<byte> data,
        ref string epc,
        ref int antennaPort,
        ref DateTimeOffset? timestampUtc,
        ref double? rssi,
        ref ushort? channelIndex,
        ref ushort? tagSeenCount,
        ref uint? accessSpecId,
        ref ushort? inventoryParamSpecId,
        ref ushort? opSpecResult)
    {
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
                        var microsSinceEpoch = BinaryPrimitives.ReadUInt64BigEndian(data[(offset + 2)..]);
                        if (microsSinceEpoch > 0)
                            timestampUtc = LlrpEpoch.AddTicks((long)(microsSinceEpoch * 10));
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

                    case TvParamTypeInventoryParameterSpecId when offset + 4 <= data.Length:
                        inventoryParamSpecId = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]);
                        break;

                    case TvParamTypeAccessSpecId when offset + 6 <= data.Length:
                        accessSpecId = BinaryPrimitives.ReadUInt32BigEndian(data[(offset + 2)..]);
                        break;

                    case TvParamTypeOpSpecResultStatus when offset + 4 <= data.Length:
                        opSpecResult = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]);
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

            if (tlvType == ParamTypeEpcData)
            {
                // EPCData TLV layout: [type(2)][length(2)][epcLengthBits(2)][epc bytes...]
                if (tlvLength > 6)
                {
                    var epcBitCount = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 4)..]);
                    var epcByteCount = Math.Min((epcBitCount + 7) / 8, tlvLength - 6);
                    epc = Convert.ToHexString(data.Slice(offset + 6, epcByteCount)).ToLowerInvariant();
                }
            }

            offset += tlvLength;
        }
    }

    /// <summary>
    /// Returns the total byte length of a TV-encoded parameter given its type.
    /// TV parameters have a fixed size defined by the LLRP specification.
    /// </summary>
    private static int GetTvParameterLength(ushort tvType) => tvType switch
    {
        TvParamTypeAntennaId               => 4,   // 2 header + 2 value
        TvParamTypeFirstSeenTimestampUtc    => 10,  // 2 header + 8 value
        TvParamTypeLastSeenTimestampUtc     => 10,  // 2 header + 8 value
        TvParamTypePeakRssi                 => 3,   // 2 header + 1 value
        TvParamTypeChannelIndex             => 4,   // 2 header + 2 value
        TvParamTypeTagSeenCount             => 4,   // 2 header + 2 value
        TvParamTypeInventoryParameterSpecId => 4,   // 2 header + 2 value
        TvParamTypeAccessSpecId             => 6,   // 2 header + 4 value
        TvParamTypeOpSpecResultStatus       => 4,   // 2 header + 2 value
        _ => 2  // Unknown TV -- skip header only (best effort).
    };
}
