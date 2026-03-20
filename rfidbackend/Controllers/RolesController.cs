using Microsoft.AspNetCore.Mvc;
using rfidbackend.Services;

namespace rfidbackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _roleService.GetAllAsync();
        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var role = await _roleService.GetByIdAsync(id);
        if (role == null) return NotFound();
        return Ok(role);
    }

    [HttpGet("{id}/permissions")]
    public async Task<IActionResult> GetWithPermissions(int id)
    {
        var role = await _roleService.GetWithPermissionsAsync(id);
        if (role == null) return NotFound();
        return Ok(role);
    }
}
