namespace Rfid.IngestionService.Llrp;

// ??????????????????????????????????????????????????????????????
// ROSpec domain models
// These are lightweight in-process representations of LLRP ROSpec
// structures. They will be serialised into binary LLRP messages
// once protocol-level encoding is implemented.
// ??????????????????????????????????????????????????????????????

/// <summary>
/// Defines when the reader should start executing inventory rounds.
/// </summary>
public enum RoSpecStartTriggerType
{
    /// <summary>Inventory begins immediately when the ROSpec is enabled.</summary>
    Immediate = 0,

    /// <summary>Inventory begins in response to a periodic timer.</summary>
    Periodic = 1,

    /// <summary>Inventory begins when a GPI event is detected.</summary>
    GpiTrigger = 2
}

/// <summary>
/// Defines when the reader should stop the current inventory round.
/// </summary>
public enum RoSpecStopTriggerType
{
    /// <summary>No automatic stop; inventory runs until explicitly disabled.</summary>
    None = 0,

    /// <summary>Stop after a fixed duration.</summary>
    Duration = 1,

    /// <summary>Stop when a GPI event is detected.</summary>
    GpiTrigger = 2
}

/// <summary>
/// Controls when inventory rounds begin.
/// </summary>
public sealed class RoSpecStartTrigger
{
    /// <summary>Type of start trigger.</summary>
    public RoSpecStartTriggerType TriggerType { get; set; } = RoSpecStartTriggerType.Immediate;

    /// <summary>
    /// Period in milliseconds when <see cref="TriggerType"/> is
    /// <see cref="RoSpecStartTriggerType.Periodic"/>.
    /// Ignored for other trigger types.
    /// </summary>
    public uint PeriodicTriggerValueMs { get; set; }

    /// <summary>GPI port number for <see cref="RoSpecStartTriggerType.GpiTrigger"/>.</summary>
    public ushort GpiPortNumber { get; set; }

    /// <summary>Whether the GPI trigger fires on high (<c>true</c>) or low (<c>false</c>).</summary>
    public bool GpiTriggerOnHigh { get; set; } = true;
}

/// <summary>
/// Controls when an active inventory round ends.
/// </summary>
public sealed class RoSpecStopTrigger
{
    /// <summary>Type of stop trigger.</summary>
    public RoSpecStopTriggerType TriggerType { get; set; } = RoSpecStopTriggerType.None;

    /// <summary>
    /// Duration in milliseconds when <see cref="TriggerType"/> is
    /// <see cref="RoSpecStopTriggerType.Duration"/>.
    /// Ignored for other trigger types.
    /// </summary>
    public uint DurationMs { get; set; }

    /// <summary>GPI port number for <see cref="RoSpecStopTriggerType.GpiTrigger"/>.</summary>
    public ushort GpiPortNumber { get; set; }

    /// <summary>Whether the GPI trigger fires on high (<c>true</c>) or low (<c>false</c>).</summary>
    public bool GpiTriggerOnHigh { get; set; } = true;
}

/// <summary>
/// Represents the inventory parameters applied to one or more antennas.
/// Maps conceptually to an LLRP AISpec (Antenna Inventory Spec).
/// </summary>
public sealed class AntennaInventorySpec
{
    /// <summary>
    /// Antenna port numbers to include in the inventory.
    /// An empty list means "all antennas" per the LLRP specification.
    /// </summary>
    public List<ushort> AntennaIds { get; set; } = [];

    /// <summary>
    /// Transmit power index as defined in the reader's capability report.
    /// This is a placeholder; the actual value must match the reader's
    /// supported power table.
    /// </summary>
    public ushort TransmitPowerIndex { get; set; } = 1;

    /// <summary>Inventory mode index from the reader's capability report.</summary>
    public ushort ModeIndex { get; set; }

    /// <summary>
    /// Session number (0-3) used during the inventory round.
    /// Controls how tags transition between A and B states.
    /// </summary>
    public byte Session { get; set; }

    /// <summary>Estimated tag population in the field of view, used for Q-algorithm tuning.</summary>
    public ushort TagPopulationEstimate { get; set; } = 32;

    /// <summary>Per-antenna receive sensitivity threshold index. 0 = use reader default.</summary>
    public ushort ReceiveSensitivityIndex { get; set; }
}

/// <summary>
/// Controls when the reader sends tag-report (RO_ACCESS_REPORT) messages.
/// </summary>
public enum RoReportTrigger
{
    /// <summary>Report is sent when the ROSpec completes or is stopped.</summary>
    EndOfRoSpec = 0,

    /// <summary>Report is sent at the end of each AISpec within the ROSpec.</summary>
    EndOfAiSpec = 1,

    /// <summary>Report is sent after every N tag observations (reader-defined).</summary>
    UponNTags = 2
}

/// <summary>
/// Bit-flags that select which optional fields the reader includes in each
/// TagReportData entry inside an RO_ACCESS_REPORT message.
/// </summary>
[Flags]
public enum TagReportContentFlags
{
    /// <summary>No optional fields.</summary>
    None = 0,

    /// <summary>Include the AntennaID parameter.</summary>
    AntennaId = 1 << 0,

    /// <summary>Include the FirstSeenTimestampUTC parameter.</summary>
    FirstSeenTimestamp = 1 << 1,

    /// <summary>Include the LastSeenTimestampUTC parameter.</summary>
    LastSeenTimestamp = 1 << 2,

    /// <summary>Include the PeakRSSI parameter.</summary>
    PeakRssi = 1 << 3,

    /// <summary>Include the ChannelIndex parameter.</summary>
    ChannelIndex = 1 << 4,

    /// <summary>Include the TagSeenCount parameter.</summary>
    TagSeenCount = 1 << 5,

    /// <summary>Include the AccessSpecID parameter.</summary>
    AccessSpecId = 1 << 6,

    /// <summary>Convenience: include all optional fields.</summary>
    All = AntennaId | FirstSeenTimestamp | LastSeenTimestamp | PeakRssi
        | ChannelIndex | TagSeenCount | AccessSpecId
}

/// <summary>
/// In-process representation of an LLRP ROSpec (Reader Operation Specification).
/// Describes a complete inventory operation: when to start, what antennas
/// to use, and when to stop.
/// </summary>
public sealed class RoSpec
{
    /// <summary>Unique identifier for this ROSpec (1–based).</summary>
    public uint RoSpecId { get; set; } = 1;

    /// <summary>
    /// Priority of this ROSpec relative to others on the same reader.
    /// 0 is the lowest priority; 7 is the highest.
    /// </summary>
    public byte Priority { get; set; }

    /// <summary>Defines how inventory rounds are initiated.</summary>
    public RoSpecStartTrigger StartTrigger { get; set; } = new();

    /// <summary>Defines how an active inventory round is terminated.</summary>
    public RoSpecStopTrigger StopTrigger { get; set; } = new();

    /// <summary>Antenna and inventory parameters for this operation.</summary>
    public AntennaInventorySpec AntennaSpec { get; set; } = new();

    /// <summary>
    /// Controls when the reader sends tag-report notifications.
    /// Defaults to reporting at the end of each AISpec.
    /// </summary>
    public RoReportTrigger ReportTrigger { get; set; } = RoReportTrigger.EndOfAiSpec;

    /// <summary>Selects which optional fields are included in each tag report.</summary>
    public TagReportContentFlags ReportContentFlags { get; set; } = TagReportContentFlags.All;
}

// ??????????????????????????????????????????????????????????????
// Builder
// ??????????????????????????????????????????????????????????????

/// <summary>
/// Builds and configures <see cref="RoSpec"/> definitions that control
/// how an RFID reader performs tag inventory.
/// <para>
/// This builder produces in-process configuration objects only.
/// It does <b>not</b> communicate with the reader or perform LLRP encoding.
/// Protocol-level serialisation will be added in a future iteration.
/// </para>
/// </summary>
public class RospecBuilder
{
    private uint _roSpecId = 1;
    private byte _priority;
    private readonly List<ushort> _antennaIds = [];
    private ushort _transmitPowerIndex = 1;
    private RoSpecStartTriggerType _startTriggerType = RoSpecStartTriggerType.Immediate;
    private RoSpecStopTriggerType _stopTriggerType = RoSpecStopTriggerType.None;
    private uint _stopDurationMs;
    private RoReportTrigger _reportTrigger = RoReportTrigger.EndOfAiSpec;
    private TagReportContentFlags _reportContentFlags = TagReportContentFlags.All;

    /// <summary>
    /// Sets the ROSpec identifier. Must be unique per reader.
    /// </summary>
    public RospecBuilder WithRoSpecId(uint roSpecId)
    {
        _roSpecId = roSpecId;
        return this;
    }

    /// <summary>
    /// Sets the ROSpec priority (0–7).
    /// </summary>
    public RospecBuilder WithPriority(byte priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// Configures the antenna ports to include in the inventory.
    /// Pass no values to include all antennas.
    /// </summary>
    public RospecBuilder WithAntennas(params ushort[] antennaIds)
    {
        _antennaIds.Clear();
        _antennaIds.AddRange(antennaIds);
        return this;
    }

    /// <summary>
    /// Sets the transmit power index (must match the reader's capability table).
    /// </summary>
    public RospecBuilder WithTransmitPowerIndex(ushort index)
    {
        _transmitPowerIndex = index;
        return this;
    }

    /// <summary>
    /// Configures the start trigger type for the ROSpec.
    /// </summary>
    public RospecBuilder WithStartTrigger(RoSpecStartTriggerType triggerType)
    {
        _startTriggerType = triggerType;
        return this;
    }

    /// <summary>
    /// Configures the stop trigger type and optional duration.
    /// </summary>
    public RospecBuilder WithStopTrigger(RoSpecStopTriggerType triggerType, uint durationMs = 0)
    {
        _stopTriggerType = triggerType;
        _stopDurationMs = durationMs;
        return this;
    }

    /// <summary>
    /// Configures when the reader sends tag-report (RO_ACCESS_REPORT) messages.
    /// </summary>
    public RospecBuilder WithReportTrigger(RoReportTrigger trigger)
    {
        _reportTrigger = trigger;
        return this;
    }

    /// <summary>
    /// Configures which optional fields are included in each TagReportData entry.
    /// </summary>
    public RospecBuilder WithReportContentFlags(TagReportContentFlags flags)
    {
        _reportContentFlags = flags;
        return this;
    }

    /// <summary>
    /// Builds a <see cref="RoSpec"/> using the currently configured values.
    /// </summary>
    public RoSpec Build()
    {
        if (_stopTriggerType == RoSpecStopTriggerType.Duration && _stopDurationMs == 0)
            throw new InvalidOperationException("Duration stop trigger requires a non-zero DurationMs value.");

        if (_priority > 7)
            throw new ArgumentOutOfRangeException(nameof(_priority), "ROSpec priority must be between 0 and 7.");

        return new RoSpec
        {
            RoSpecId = _roSpecId,
            Priority = _priority,
            StartTrigger = new RoSpecStartTrigger
            {
                TriggerType = _startTriggerType
            },
            StopTrigger = new RoSpecStopTrigger
            {
                TriggerType = _stopTriggerType,
                DurationMs = _stopDurationMs
            },
            AntennaSpec = new AntennaInventorySpec
            {
                AntennaIds = [.. _antennaIds],
                TransmitPowerIndex = _transmitPowerIndex
            },
            ReportTrigger = _reportTrigger,
            ReportContentFlags = _reportContentFlags
        };
    }

    /// <summary>
    /// Builds a sensible default ROSpec for continuous inventory on all antennas.
    /// <para>
    /// Defaults:
    /// <list type="bullet">
    ///   <item>ROSpec ID: 1</item>
    ///   <item>Priority: 0 (lowest)</item>
    ///   <item>Start trigger: Immediate</item>
    ///   <item>Stop trigger: None (runs until disabled)</item>
    ///   <item>Antennas: all (empty list)</item>
    ///   <item>Transmit power index: 1</item>
    /// </list>
    /// </para>
    /// </summary>
    public static RoSpec BuildDefaultRospec()
    {
        return new RospecBuilder()
            .WithRoSpecId(1)
            .WithPriority(0)
            .WithAntennas()            // all antennas
            .WithTransmitPowerIndex(1) // first entry in the reader's power table
            .WithStartTrigger(RoSpecStartTriggerType.Immediate)
            .WithStopTrigger(RoSpecStopTriggerType.None)
            .WithReportTrigger(RoReportTrigger.EndOfAiSpec)
            .WithReportContentFlags(TagReportContentFlags.All)
            .Build();
    }
}
