namespace Rfid.WebApi.Entities;

public enum TicketStatus
{
    Open,
    Closed
}

public enum TicketType
{
    RequestTool,
    MaintenanceRequired
}

public class Ticket
{
    public int Id { get; set; }
    public TicketType Type { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    public int ReasonForRequestId { get; set; }
    public ReasonForRequest ReasonForRequest { get; set; } = null!;

    public int ToolTypeId { get; set; }
    public ToolType ToolType { get; set; } = null!;

    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ToolAssignment? ToolAssignment { get; set; }
}
