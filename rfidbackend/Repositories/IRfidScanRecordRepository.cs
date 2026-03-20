using rfidbackend.Entities;

namespace rfidbackend.Repositories;

public interface IRfidScanRecordRepository : IRepository<RfidScanRecord>
{
    Task<IEnumerable<RfidScanRecord>> GetByTagIdAsync(string tagId);
    Task<IEnumerable<RfidScanRecord>> GetByDateRangeAsync(DateTimeOffset start, DateTimeOffset end);
}
