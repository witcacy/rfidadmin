using rfidbackend.Entities;

namespace rfidbackend.Services;

public interface IToolAssignmentService
{
    Task<ToolAssignment> AssignToolAsync(string badgeId, string rfidTag, int? ticketId);
    Task<ToolAssignment?> ReturnToolAsync(int assignmentId);
    Task<IEnumerable<ToolAssignment>> GetActiveByUserAsync(int userId);
}
