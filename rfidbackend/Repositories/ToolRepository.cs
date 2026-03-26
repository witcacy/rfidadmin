using Microsoft.EntityFrameworkCore;
using Rfid.WebApi.Data;
using Rfid.WebApi.Entities;

namespace Rfid.WebApi.Repositories;

public class ToolRepository : Repository<Tool>, IToolRepository
{
    public ToolRepository(RfidDbContext context) : base(context) { }

    public async Task<Tool?> GetByRfidTagAsync(string rfidTag) =>
        await _dbSet
            .Include(t => t.ToolType)
            .Include(t => t.Area)
            .FirstOrDefaultAsync(t => t.RfidTag == rfidTag);

    public async Task<Tool?> GetWithDetailsAsync(int id) =>
        await _dbSet
            .Include(t => t.ToolType)
            .Include(t => t.Area)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<IEnumerable<Tool>> GetByStatusAsync(ToolStatus status) =>
        await _dbSet
            .Include(t => t.ToolType)
            .Include(t => t.Area)
            .Where(t => t.Status == status)
            .ToListAsync();

    public async Task<IEnumerable<Tool>> GetByAreaAsync(int areaId) =>
        await _dbSet
            .Include(t => t.ToolType)
            .Where(t => t.AreaId == areaId)
            .ToListAsync();
}
