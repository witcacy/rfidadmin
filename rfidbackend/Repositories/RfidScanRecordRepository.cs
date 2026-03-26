using Microsoft.EntityFrameworkCore;
using Rfid.WebApi.Data;
using Rfid.WebApi.Entities;

namespace Rfid.WebApi.Repositories;

public class RfidScanRecordRepository : Repository<RfidScanRecord>, IRfidScanRecordRepository
{
    public RfidScanRecordRepository(RfidDbContext context) : base(context) { }

    public async Task<IEnumerable<RfidScanRecord>> GetByTagIdAsync(string tagId) =>
        await _dbSet
            .Where(r => r.TagId == tagId)
            .OrderByDescending(r => r.ScannedAt)
            .ToListAsync();

    public async Task<IEnumerable<RfidScanRecord>> GetByDateRangeAsync(DateTimeOffset start, DateTimeOffset end) =>
        await _dbSet
            .Where(r => r.ScannedAt >= start && r.ScannedAt <= end)
            .OrderByDescending(r => r.ScannedAt)
            .ToListAsync();
}
