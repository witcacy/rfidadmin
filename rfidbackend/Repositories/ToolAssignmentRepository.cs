using Microsoft.EntityFrameworkCore;
using rfidbackend.Data;
using rfidbackend.Entities;

namespace rfidbackend.Repositories;

public class ToolAssignmentRepository : Repository<ToolAssignment>, IToolAssignmentRepository
{
    public ToolAssignmentRepository(RfidDbContext context) : base(context) { }

    public async Task<IEnumerable<ToolAssignment>> GetActiveByUserAsync(int userId) =>
        await _dbSet
            .Include(ta => ta.Tool).ThenInclude(t => t.ToolType)
            .Include(ta => ta.Tool).ThenInclude(t => t.Area)
            .Where(ta => ta.UserId == userId && ta.ReturnedAt == null)
            .ToListAsync();

    public async Task<ToolAssignment?> GetActiveByToolAsync(int toolId) =>
        await _dbSet
            .Include(ta => ta.User)
            .Include(ta => ta.Tool)
            .FirstOrDefaultAsync(ta => ta.ToolId == toolId && ta.ReturnedAt == null);
}
