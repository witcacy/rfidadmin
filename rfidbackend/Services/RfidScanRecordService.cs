using rfidbackend.Entities;
using rfidbackend.Repositories;

namespace rfidbackend.Services;

public class RfidScanRecordService : IRfidScanRecordService
{
    private readonly IRfidScanRecordRepository _scanRepository;

    public RfidScanRecordService(IRfidScanRecordRepository scanRepository)
    {
        _scanRepository = scanRepository;
    }

    public async Task<RfidScanRecord> RecordScanAsync(string tagId, string? antennaId)
    {
        var record = new RfidScanRecord
        {
            TagId = tagId,
            AntennaId = antennaId
        };

        await _scanRepository.AddAsync(record);
        await _scanRepository.SaveChangesAsync();
        return record;
    }

    public async Task<IEnumerable<RfidScanRecord>> GetByTagIdAsync(string tagId) =>
        await _scanRepository.GetByTagIdAsync(tagId);

    public async Task<IEnumerable<RfidScanRecord>> GetByDateRangeAsync(DateTimeOffset start, DateTimeOffset end) =>
        await _scanRepository.GetByDateRangeAsync(start, end);
}
