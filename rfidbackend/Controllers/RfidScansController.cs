using Microsoft.AspNetCore.Mvc;
using rfidbackend.DTOs;
using rfidbackend.Services;

namespace rfidbackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RfidScansController : ControllerBase
{
    private readonly IRfidScanRecordService _scanService;

    public RfidScansController(IRfidScanRecordService scanService)
    {
        _scanService = scanService;
    }

    [HttpGet("tag/{tagId}")]
    public async Task<IActionResult> GetByTagId(string tagId)
    {
        var records = await _scanService.GetByTagIdAsync(tagId);
        return Ok(records);
    }

    [HttpGet("range")]
    public async Task<IActionResult> GetByDateRange([FromQuery] DateTimeOffset start, [FromQuery] DateTimeOffset end)
    {
        var records = await _scanService.GetByDateRangeAsync(start, end);
        return Ok(records);
    }

    [HttpPost]
    public async Task<IActionResult> RecordScan([FromBody] RecordScanRequest request)
    {
        var record = await _scanService.RecordScanAsync(request.TagId, request.AntennaId);
        return CreatedAtAction(null, new { id = record.Id }, record);
    }
}
