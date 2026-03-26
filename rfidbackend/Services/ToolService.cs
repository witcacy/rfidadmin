using Rfid.WebApi.Entities;
using Rfid.WebApi.Repositories;

namespace Rfid.WebApi.Services;

public class ToolService : IToolService
{
    private readonly IToolRepository _toolRepository;
    private readonly IToolRemovalRepository _toolRemovalRepository;
    private readonly IRepository<Area> _areaRepository;

    public ToolService(
        IToolRepository toolRepository,
        IToolRemovalRepository toolRemovalRepository,
        IRepository<Area> areaRepository)
    {
        _toolRepository = toolRepository;
        _toolRemovalRepository = toolRemovalRepository;
        _areaRepository = areaRepository;
    }

    public async Task<IEnumerable<Tool>> GetAllAsync() =>
        await _toolRepository.GetAllAsync();

    public async Task<Tool?> GetByIdAsync(int id) =>
        await _toolRepository.GetWithDetailsAsync(id);

    public async Task<Tool?> GetByRfidTagAsync(string rfidTag) =>
        await _toolRepository.GetByRfidTagAsync(rfidTag);

    public async Task<IEnumerable<Tool>> GetByStatusAsync(ToolStatus status) =>
        await _toolRepository.GetByStatusAsync(status);

    public async Task<Tool> CreateAsync(int toolTypeId, string serialNumber, string description, string rfidTag)
    {
        var existing = await _toolRepository.GetByRfidTagAsync(rfidTag);
        if (existing != null)
            throw new InvalidOperationException($"A tool with RFID tag '{rfidTag}' already exists.");

        var engineeringNew = (await _areaRepository.FindAsync(a => a.Name == "Engineering - New")).FirstOrDefault();

        var tool = new Tool
        {
            ToolTypeId = toolTypeId,
            SerialNumber = serialNumber,
            Description = description,
            RfidTag = rfidTag,
            Status = ToolStatus.Active,
            AreaId = engineeringNew?.Id ?? 1
        };

        await _toolRepository.AddAsync(tool);
        await _toolRepository.SaveChangesAsync();
        return tool;
    }

    public async Task<Tool?> RemoveToolAsync(int toolId, int reasonForRequestId, string rfidTag)
    {
        var tool = await _toolRepository.GetByIdAsync(toolId);
        if (tool == null) return null;

        tool.Status = ToolStatus.InActive;

        var outOfService = (await _areaRepository.FindAsync(a => a.Name == "Out of Service")).FirstOrDefault();
        if (outOfService != null)
            tool.AreaId = outOfService.Id;

        _toolRepository.Update(tool);

        var removal = new ToolRemoval
        {
            ToolId = toolId,
            ReasonForRequestId = reasonForRequestId,
            RfidTag = rfidTag
        };

        await _toolRemovalRepository.AddAsync(removal);
        await _toolRemovalRepository.SaveChangesAsync();
        return tool;
    }
}
