using Rfid.WebApi.Entities;

namespace Rfid.WebApi.Repositories;

public interface IRfidScanRecordRepository : IRepository<RfidScanRecord>
{
    Task<IEnumerable<RfidScanRecord>> GetByTagIdAsync(string tagId);
    Task<IEnumerable<RfidScanRecord>> GetByDateRangeAsync(DateTimeOffset start, DateTimeOffset end);
}
