using Rfid.WebApi.Entities;

namespace Rfid.WebApi.Services;

public interface ITicketService
{
    Task<IEnumerable<Ticket>> GetAllAsync();
    Task<IEnumerable<Ticket>> GetOpenTicketsAsync();
    Task<Ticket?> GetByIdAsync(int id);
    Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status);
    Task<IEnumerable<Ticket>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task<Ticket> CreateRequestToolTicketAsync(int reasonId, int areaId, int toolTypeId, int userId);
    Task<Ticket> CreateMaintenanceTicketAsync(int reasonId, int toolTypeId, int areaId, int userId);
    Task<Ticket?> CloseTicketAsync(int id);
}
