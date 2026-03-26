using Rfid.WebApi.Entities;
using Rfid.WebApi.Repositories;

namespace Rfid.WebApi.Services;

public class ToolAssignmentService : IToolAssignmentService
{
    private readonly IToolAssignmentRepository _assignmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IToolRepository _toolRepository;

    public ToolAssignmentService(
        IToolAssignmentRepository assignmentRepository,
        IUserRepository userRepository,
        IToolRepository toolRepository)
    {
        _assignmentRepository = assignmentRepository;
        _userRepository = userRepository;
        _toolRepository = toolRepository;
    }

    public async Task<ToolAssignment> AssignToolAsync(string badgeId, string rfidTag, int? ticketId)
    {
        var user = await _userRepository.GetByBadgeIdAsync(badgeId)
            ?? throw new InvalidOperationException($"User with badge '{badgeId}' not found.");

        var tool = await _toolRepository.GetByRfidTagAsync(rfidTag)
            ?? throw new InvalidOperationException($"Tool with RFID tag '{rfidTag}' not found.");

        var active = await _assignmentRepository.GetActiveByToolAsync(tool.Id);
        if (active != null)
            throw new InvalidOperationException($"Tool '{rfidTag}' is already assigned to another user.");

        tool.Status = ToolStatus.InUse;
        _toolRepository.Update(tool);

        var assignment = new ToolAssignment
        {
            UserId = user.Id,
            ToolId = tool.Id,
            BadgeId = badgeId,
            RfidTag = rfidTag,
            TicketId = ticketId
        };

        await _assignmentRepository.AddAsync(assignment);
        await _assignmentRepository.SaveChangesAsync();
        return assignment;
    }

    public async Task<ToolAssignment?> ReturnToolAsync(int assignmentId)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
        if (assignment == null) return null;

        assignment.ReturnedAt = DateTime.UtcNow;
        _assignmentRepository.Update(assignment);

        var tool = await _toolRepository.GetByIdAsync(assignment.ToolId);
        if (tool != null)
        {
            tool.Status = ToolStatus.Active;
            _toolRepository.Update(tool);
        }

        await _assignmentRepository.SaveChangesAsync();
        return assignment;
    }

    public async Task<IEnumerable<ToolAssignment>> GetActiveByUserAsync(int userId) =>
        await _assignmentRepository.GetActiveByUserAsync(userId);
}
