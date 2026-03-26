namespace Rfid.WebApi.Entities;

public class ReasonForRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Ticket> Tickets { get; set; } = [];
    public ICollection<ToolRemoval> ToolRemovals { get; set; } = [];
}
