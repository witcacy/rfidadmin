namespace Rfid.WebApi.DTOs;

public record CreateToolRequest(int ToolTypeId, string SerialNumber, string Description, string RfidTag);
public record RemoveToolRequest(int ToolId, int ReasonForRequestId, string RfidTag);
