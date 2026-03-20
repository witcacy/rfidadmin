namespace rfidbackend.Entities;

public class RfidScanRecord
{
    public int Id { get; set; }
    public string TagId { get; set; } = string.Empty;
    public string? AntennaId { get; set; }
    public DateTimeOffset ScannedAt { get; set; } = DateTimeOffset.UtcNow;
}
