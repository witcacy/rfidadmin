namespace Rfid.WebApi.DTOs;

public record AssignToolRequest(string BadgeId, string RfidTag, int? TicketId);
