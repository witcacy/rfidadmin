using Rfid.WebApi.Entities;

namespace Rfid.WebApi.Services;

public interface IRfidScanRecordService
{
    Task<RfidScanRecord> RecordScanAsync(string tagId, string? antennaId);
    Task<IEnumerable<RfidScanRecord>> GetByTagIdAsync(string tagId);
    Task<IEnumerable<RfidScanRecord>> GetByDateRangeAsync(DateTimeOffset start, DateTimeOffset end);
}
