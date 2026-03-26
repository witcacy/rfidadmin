namespace Rfid.WebApi.DTOs;

public record RecordScanRequest(string TagId, string? AntennaId);
