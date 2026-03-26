namespace Rfid.WebApi.DTOs;

public record CreateUserRequest(string FullName, string EmployeeId, string BadgeId, string Department, int RoleId);
public record UpdateUserRequest(string FullName, string Department, int RoleId);
