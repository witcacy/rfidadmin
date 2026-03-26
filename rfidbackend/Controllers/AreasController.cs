using Microsoft.AspNetCore.Mvc;
using Rfid.WebApi.DTOs;
using Rfid.WebApi.Entities;
using Rfid.WebApi.Services;

namespace Rfid.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AreasController : ControllerBase
{
    private readonly ICatalogService<Area> _catalogService;

    public AreasController(ICatalogService<Area> catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _catalogService.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _catalogService.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCatalogRequest request)
    {
        var item = await _catalogService.CreateAsync(new Area { Name = request.Name });
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _catalogService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
