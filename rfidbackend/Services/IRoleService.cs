using rfidbackend.Entities;

namespace rfidbackend.Services;

public interface IRoleService
{
    Task<IEnumerable<Role>> GetAllAsync();
    Task<Role?> GetByIdAsync(int id);
    Task<Role?> GetWithPermissionsAsync(int id);
}
