using Microsoft.EntityFrameworkCore;
using rfidbackend.Data;
using rfidbackend.Entities;

namespace rfidbackend.Repositories;

public class ToolRemovalRepository : Repository<ToolRemoval>, IToolRemovalRepository
{
    public ToolRemovalRepository(RfidDbContext context) : base(context) { }

    public async Task<IEnumerable<ToolRemoval>> GetByToolAsync(int toolId) =>
        await _dbSet
            .Include(tr => tr.ReasonForRequest)
            .Include(tr => tr.Tool).ThenInclude(t => t.ToolType)
            .Where(tr => tr.ToolId == toolId)
            .OrderByDescending(tr => tr.RemovedAt)
            .ToListAsync();
}
