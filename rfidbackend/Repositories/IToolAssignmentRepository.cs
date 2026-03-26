using Rfid.WebApi.Entities;

namespace Rfid.WebApi.Repositories;

public interface IToolAssignmentRepository : IRepository<ToolAssignment>
{
    Task<IEnumerable<ToolAssignment>> GetActiveByUserAsync(int userId);
    Task<ToolAssignment?> GetActiveByToolAsync(int toolId);
}
