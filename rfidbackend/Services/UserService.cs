using Rfid.WebApi.Entities;
using Rfid.WebApi.Repositories;

namespace Rfid.WebApi.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<User>> GetAllAsync() =>
        await _userRepository.GetAllAsync();

    public async Task<User?> GetByIdAsync(int id) =>
        await _userRepository.GetWithRoleAsync(id);

    public async Task<User?> GetByBadgeIdAsync(string badgeId) =>
        await _userRepository.GetByBadgeIdAsync(badgeId);

    public async Task<User> CreateAsync(string fullName, string employeeId, string badgeId, string department, int roleId)
    {
        var existing = await _userRepository.GetByEmployeeIdAsync(employeeId);
        if (existing != null)
            throw new InvalidOperationException($"User with EmployeeId '{employeeId}' already exists.");

        var user = new User
        {
            FullName = fullName,
            EmployeeId = employeeId,
            BadgeId = badgeId,
            Department = department,
            RoleId = roleId
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        return user;
    }

    public async Task<User?> UpdateAsync(int id, string fullName, string department, int roleId)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return null;

        user.FullName = fullName;
        user.Department = department;
        user.RoleId = roleId;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();
        return true;
    }
}
