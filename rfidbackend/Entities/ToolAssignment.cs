namespace Rfid.WebApi.Entities;

public class ToolAssignment
{
    public int Id { get; set; }
    public string BadgeId { get; set; } = string.Empty;
    public string RfidTag { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAt { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int ToolId { get; set; }
    public Tool Tool { get; set; } = null!;

    public int? TicketId { get; set; }
    public Ticket? Ticket { get; set; }
}
