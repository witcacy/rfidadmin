using Microsoft.EntityFrameworkCore;
using Rfid.WebApi.Data;
using Rfid.WebApi.Entities;

namespace Rfid.WebApi.Repositories;

public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(RfidDbContext context) : base(context) { }

    public async Task<Role?> GetByNameAsync(string name) =>
        await _dbSet.FirstOrDefaultAsync(r => r.Name == name);

    public async Task<Role?> GetWithPermissionsAsync(int id) =>
        await _dbSet
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);
}
