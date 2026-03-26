namespace Rfid.WebApi.DTOs;

public record CreateRequestToolTicketRequest(int ReasonForRequestId, int AreaId, int ToolTypeId, int CreatedByUserId);
public record CreateMaintenanceTicketRequest(int ReasonForRequestId, int ToolTypeId, int AreaId, int CreatedByUserId);
