using Microsoft.AspNetCore.Mvc;
using Rfid.WebApi.DTOs;
using Rfid.WebApi.Entities;
using Rfid.WebApi.Services;

namespace Rfid.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tickets = await _ticketService.GetAllAsync();
        return Ok(tickets);
    }

    [HttpGet("open")]
    public async Task<IActionResult> GetOpen()
    {
        var tickets = await _ticketService.GetOpenTicketsAsync();
        return Ok(tickets);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ticket = await _ticketService.GetByIdAsync(id);
        if (ticket == null) return NotFound();
        return Ok(ticket);
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(TicketStatus status)
    {
        var tickets = await _ticketService.GetByStatusAsync(status);
        return Ok(tickets);
    }

    [HttpGet("report")]
    public async Task<IActionResult> GetByDateRange([FromQuery] ReportRequest request)
    {
        var tickets = await _ticketService.GetByDateRangeAsync(request.StartDate, request.EndDate);

        if (!string.IsNullOrEmpty(request.Status) && request.Status != "All")
        {
            var status = Enum.Parse<TicketStatus>(request.Status, true);
            tickets = tickets.Where(t => t.Status == status);
        }

        return Ok(tickets);
    }

    [HttpPost("request-tool")]
    public async Task<IActionResult> CreateRequestTool([FromBody] CreateRequestToolTicketRequest request)
    {
        var ticket = await _ticketService.CreateRequestToolTicketAsync(
            request.ReasonForRequestId, request.AreaId, request.ToolTypeId, request.CreatedByUserId);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    [HttpPost("maintenance")]
    public async Task<IActionResult> CreateMaintenance([FromBody] CreateMaintenanceTicketRequest request)
    {
        var ticket = await _ticketService.CreateMaintenanceTicketAsync(
            request.ReasonForRequestId, request.ToolTypeId, request.AreaId, request.CreatedByUserId);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    [HttpPatch("{id}/close")]
    public async Task<IActionResult> Close(int id)
    {
        var ticket = await _ticketService.CloseTicketAsync(id);
        if (ticket == null) return NotFound();
        return Ok(ticket);
    }
}
