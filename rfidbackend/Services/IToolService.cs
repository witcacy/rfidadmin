using rfidbackend.Entities;

namespace rfidbackend.Services;

public interface IToolService
{
    Task<IEnumerable<Tool>> GetAllAsync();
    Task<Tool?> GetByIdAsync(int id);
    Task<Tool?> GetByRfidTagAsync(string rfidTag);
    Task<IEnumerable<Tool>> GetByStatusAsync(ToolStatus status);
    Task<Tool> CreateAsync(int toolTypeId, string serialNumber, string description, string rfidTag);
    Task<Tool?> RemoveToolAsync(int toolId, int reasonForRequestId, string rfidTag);
}
