using rfidbackend.Entities;

namespace rfidbackend.Repositories;

public interface IToolRemovalRepository : IRepository<ToolRemoval>
{
    Task<IEnumerable<ToolRemoval>> GetByToolAsync(int toolId);
}
