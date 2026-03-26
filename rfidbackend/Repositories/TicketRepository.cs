using Microsoft.EntityFrameworkCore;
using Rfid.WebApi.Data;
using Rfid.WebApi.Entities;

namespace Rfid.WebApi.Repositories;

public class TicketRepository : Repository<Ticket>, ITicketRepository
{
    public TicketRepository(RfidDbContext context) : base(context) { }

    public async Task<IEnumerable<Ticket>> GetOpenTicketsAsync() =>
        await _dbSet
            .Include(t => t.ReasonForRequest)
            .Include(t => t.ToolType)
            .Include(t => t.Area)
            .Include(t => t.CreatedByUser)
            .Where(t => t.Status == TicketStatus.Open)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<Ticket?> GetWithDetailsAsync(int id) =>
        await _dbSet
            .Include(t => t.ReasonForRequest)
            .Include(t => t.ToolType)
            .Include(t => t.Area)
            .Include(t => t.CreatedByUser)
            .Include(t => t.ToolAssignment)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status) =>
        await _dbSet
            .Include(t => t.ReasonForRequest)
            .Include(t => t.ToolType)
            .Include(t => t.Area)
            .Include(t => t.CreatedByUser)
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Ticket>> GetByDateRangeAsync(DateTime start, DateTime end) =>
        await _dbSet
            .Include(t => t.ReasonForRequest)
            .Include(t => t.ToolType)
            .Include(t => t.Area)
            .Include(t => t.CreatedByUser)
            .Where(t => t.CreatedAt >= start && t.CreatedAt <= end)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
}
