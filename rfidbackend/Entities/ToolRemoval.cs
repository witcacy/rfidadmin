namespace rfidbackend.Entities;

public class ToolRemoval
{
    public int Id { get; set; }
    public string RfidTag { get; set; } = string.Empty;
    public DateTime RemovedAt { get; set; } = DateTime.UtcNow;

    public int ReasonForRequestId { get; set; }
    public ReasonForRequest ReasonForRequest { get; set; } = null!;

    public int ToolId { get; set; }
    public Tool Tool { get; set; } = null!;
}
