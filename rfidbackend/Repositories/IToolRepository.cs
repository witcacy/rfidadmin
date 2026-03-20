using rfidbackend.Entities;

namespace rfidbackend.Repositories;

public interface IToolRepository : IRepository<Tool>
{
    Task<Tool?> GetByRfidTagAsync(string rfidTag);
    Task<Tool?> GetWithDetailsAsync(int id);
    Task<IEnumerable<Tool>> GetByStatusAsync(ToolStatus status);
    Task<IEnumerable<Tool>> GetByAreaAsync(int areaId);
}
