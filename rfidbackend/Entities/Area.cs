namespace rfidbackend.Entities;

public class Area
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Tool> Tools { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
}
