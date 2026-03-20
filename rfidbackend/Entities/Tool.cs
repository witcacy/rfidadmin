namespace rfidbackend.Entities;

public enum ToolStatus
{
    Active,
    InActive,
    InUse,
    OutOfService
}

public class Tool
{
    public int Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RfidTag { get; set; } = string.Empty;
    public ToolStatus Status { get; set; } = ToolStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ToolTypeId { get; set; }
    public ToolType ToolType { get; set; } = null!;

    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;

    public ICollection<ToolAssignment> ToolAssignments { get; set; } = [];
    public ICollection<ToolRemoval> ToolRemovals { get; set; } = [];
}
