using rfidbackend.Entities;

namespace rfidbackend.Repositories;

public interface ITicketRepository : IRepository<Ticket>
{
    Task<IEnumerable<Ticket>> GetOpenTicketsAsync();
    Task<Ticket?> GetWithDetailsAsync(int id);
    Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status);
    Task<IEnumerable<Ticket>> GetByDateRangeAsync(DateTime start, DateTime end);
}
