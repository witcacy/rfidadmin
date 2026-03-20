using Microsoft.AspNetCore.Mvc;
using rfidbackend.DTOs;
using rfidbackend.Entities;
using rfidbackend.Services;

namespace rfidbackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    private readonly IToolService _toolService;

    public ToolsController(IToolService toolService)
    {
        _toolService = toolService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tools = await _toolService.GetAllAsync();
        return Ok(tools);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tool = await _toolService.GetByIdAsync(id);
        if (tool == null) return NotFound();
        return Ok(tool);
    }

    [HttpGet("rfid/{rfidTag}")]
    public async Task<IActionResult> GetByRfidTag(string rfidTag)
    {
        var tool = await _toolService.GetByRfidTagAsync(rfidTag);
        if (tool == null) return NotFound();
        return Ok(tool);
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(ToolStatus status)
    {
        var tools = await _toolService.GetByStatusAsync(status);
        return Ok(tools);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateToolRequest request)
    {
        try
        {
            var tool = await _toolService.CreateAsync(
                request.ToolTypeId, request.SerialNumber, request.Description, request.RfidTag);
            return CreatedAtAction(nameof(GetById), new { id = tool.Id }, tool);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("remove")]
    public async Task<IActionResult> Remove([FromBody] RemoveToolRequest request)
    {
        var tool = await _toolService.RemoveToolAsync(request.ToolId, request.ReasonForRequestId, request.RfidTag);
        if (tool == null) return NotFound();
        return Ok(tool);
    }
}
