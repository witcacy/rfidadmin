using rfidbackend.Entities;

namespace rfidbackend.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByBadgeIdAsync(string badgeId);
    Task<User?> GetByEmployeeIdAsync(string employeeId);
    Task<User?> GetWithRoleAsync(int id);
}
