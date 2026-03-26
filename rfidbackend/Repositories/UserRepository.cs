using Microsoft.EntityFrameworkCore;
using Rfid.WebApi.Data;
using Rfid.WebApi.Entities;

namespace Rfid.WebApi.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(RfidDbContext context) : base(context) { }

    public async Task<User?> GetByBadgeIdAsync(string badgeId) =>
        await _dbSet.Include(u => u.Role).FirstOrDefaultAsync(u => u.BadgeId == badgeId);

    public async Task<User?> GetByEmployeeIdAsync(string employeeId) =>
        await _dbSet.Include(u => u.Role).FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

    public async Task<User?> GetWithRoleAsync(int id) =>
        await _dbSet.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
}
