using rfidbackend.Entities;
using rfidbackend.Repositories;

namespace rfidbackend.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;

    public RoleService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<IEnumerable<Role>> GetAllAsync() =>
        await _roleRepository.GetAllAsync();

    public async Task<Role?> GetByIdAsync(int id) =>
        await _roleRepository.GetByIdAsync(id);

    public async Task<Role?> GetWithPermissionsAsync(int id) =>
        await _roleRepository.GetWithPermissionsAsync(id);
}
