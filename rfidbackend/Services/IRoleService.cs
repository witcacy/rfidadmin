using Rfid.WebApi.Entities;

namespace Rfid.WebApi.Services;

public interface IRoleService
{
    Task<IEnumerable<Role>> GetAllAsync();
    Task<Role?> GetByIdAsync(int id);
    Task<Role?> GetWithPermissionsAsync(int id);
}
