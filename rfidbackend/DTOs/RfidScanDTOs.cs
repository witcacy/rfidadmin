namespace rfidbackend.DTOs;

public record RecordScanRequest(string TagId, string? AntennaId);
