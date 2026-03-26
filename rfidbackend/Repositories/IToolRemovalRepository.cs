using Rfid.WebApi.Entities;

namespace Rfid.WebApi.Repositories;

public interface IToolRemovalRepository : IRepository<ToolRemoval>
{
    Task<IEnumerable<ToolRemoval>> GetByToolAsync(int toolId);
}
