using Rfid.WebApi.Entities;

namespace Rfid.WebApi.Repositories;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name);
    Task<Role?> GetWithPermissionsAsync(int id);
}
