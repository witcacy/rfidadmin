using Microsoft.AspNetCore.Mvc;
using Rfid.WebApi.DTOs;
using Rfid.WebApi.Services;

namespace Rfid.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToolAssignmentsController : ControllerBase
{
    private readonly IToolAssignmentService _assignmentService;

    public ToolAssignmentsController(IToolAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetActiveByUser(int userId)
    {
        var assignments = await _assignmentService.GetActiveByUserAsync(userId);
        return Ok(assignments);
    }

    [HttpPost]
    public async Task<IActionResult> Assign([FromBody] AssignToolRequest request)
    {
        try
        {
            var assignment = await _assignmentService.AssignToolAsync(
                request.BadgeId, request.RfidTag, request.TicketId);
            return CreatedAtAction(null, new { id = assignment.Id }, assignment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/return")]
    public async Task<IActionResult> Return(int id)
    {
        var assignment = await _assignmentService.ReturnToolAsync(id);
        if (assignment == null) return NotFound();
        return Ok(assignment);
    }
}
