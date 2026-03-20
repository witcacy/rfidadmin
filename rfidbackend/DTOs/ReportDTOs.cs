namespace rfidbackend.DTOs;

public record ReportRequest(DateTime StartDate, DateTime EndDate, string? Status);
