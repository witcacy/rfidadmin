using Rfid.WebApi.Entities;

namespace Rfid.WebApi.Services;

public interface IUserService
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByBadgeIdAsync(string badgeId);
    Task<User> CreateAsync(string fullName, string employeeId, string badgeId, string department, int roleId);
    Task<User?> UpdateAsync(int id, string fullName, string department, int roleId);
    Task<bool> DeactivateAsync(int id);
}
