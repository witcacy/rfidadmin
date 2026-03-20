using rfidbackend.Entities;
using rfidbackend.Repositories;

namespace rfidbackend.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;

    public TicketService(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<IEnumerable<Ticket>> GetAllAsync() =>
        await _ticketRepository.GetAllAsync();

    public async Task<IEnumerable<Ticket>> GetOpenTicketsAsync() =>
        await _ticketRepository.GetOpenTicketsAsync();

    public async Task<Ticket?> GetByIdAsync(int id) =>
        await _ticketRepository.GetWithDetailsAsync(id);

    public async Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status) =>
        await _ticketRepository.GetByStatusAsync(status);

    public async Task<IEnumerable<Ticket>> GetByDateRangeAsync(DateTime start, DateTime end) =>
        await _ticketRepository.GetByDateRangeAsync(start, end);

    public async Task<Ticket> CreateRequestToolTicketAsync(int reasonId, int areaId, int toolTypeId, int userId)
    {
        var ticket = new Ticket
        {
            Type = TicketType.RequestTool,
            ReasonForRequestId = reasonId,
            AreaId = areaId,
            ToolTypeId = toolTypeId,
            CreatedByUserId = userId
        };

        await _ticketRepository.AddAsync(ticket);
        await _ticketRepository.SaveChangesAsync();
        return ticket;
    }

    public async Task<Ticket> CreateMaintenanceTicketAsync(int reasonId, int toolTypeId, int areaId, int userId)
    {
        var ticket = new Ticket
        {
            Type = TicketType.MaintenanceRequired,
            ReasonForRequestId = reasonId,
            ToolTypeId = toolTypeId,
            AreaId = areaId,
            CreatedByUserId = userId
        };

        await _ticketRepository.AddAsync(ticket);
        await _ticketRepository.SaveChangesAsync();
        return ticket;
    }

    public async Task<Ticket?> CloseTicketAsync(int id)
    {
        var ticket = await _ticketRepository.GetByIdAsync(id);
        if (ticket == null) return null;

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAt = DateTime.UtcNow;

        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChangesAsync();
        return ticket;
    }
}
