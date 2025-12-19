using AssetManagementApi.Data;
using AssetManagementApi.DTOs;
using AssetManagementApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AssetManagementApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DepartmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DepartmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/departments
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments()
    {
        var departmentsFromDb = await _context.Departments
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync();

        var departments = departmentsFromDb.Select(d => new DepartmentDto(
            d.Id,
            d.Name,
            d.Code,
            d.Description,
            d.IsActive,
            d.CreatedAt
        )).ToList();

        return Ok(departments);
    }

    // GET: api/departments/5
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<DepartmentDto>> GetDepartment(int id)
    {
        var departmentFromDb = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

        if (departmentFromDb == null)
            return NotFound(new { message = "დეპარტამენტი არ მოიძებნა" });

        var department = new DepartmentDto(
            departmentFromDb.Id,
            departmentFromDb.Name,
            departmentFromDb.Code,
            departmentFromDb.Description,
            departmentFromDb.IsActive,
            departmentFromDb.CreatedAt
        );

        return Ok(department);
    }

    // POST: api/departments
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment([FromBody] DepartmentCreateDto request)
    {
        // კოდის უნიკალურობა (თუ Code შევსებულია)
        if (!string.IsNullOrEmpty(request.Code))
        {
            if (await _context.Departments.AnyAsync(d => d.Code == request.Code))
                return Conflict(new { message = "ეს კოდი უკვე გამოყენებულია" });
        }

        var newDepartment = new Department
        {
            Name = request.Name.Trim(),
            Code = string.IsNullOrEmpty(request.Code) ? null : request.Code.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system"
        };

        _context.Departments.Add(newDepartment);
        await _context.SaveChangesAsync();

        var dto = new DepartmentDto(
            newDepartment.Id,
            newDepartment.Name,
            newDepartment.Code,
            newDepartment.Description,
            newDepartment.IsActive,
            newDepartment.CreatedAt
        );

        return CreatedAtAction(nameof(GetDepartment), new { id = newDepartment.Id }, dto);
    }

    // PUT: api/departments/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] CategoryUpdateDto request)  // შეცდომა! უნდა იყოს DepartmentUpdateDto
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
            return NotFound(new { message = "დეპარტამენტი არ მოიძებნა" });

        // კოდის უნიკალურობა (გარდა საკუთარი თავისა)
        if (!string.IsNullOrEmpty(request.Code) && request.Code != department.Code)
        {
            if (await _context.Departments.AnyAsync(d => d.Code == request.Code))
                return Conflict(new { message = "ეს კოდი უკვე გამოყენებულია" });
        }

        if (!string.IsNullOrEmpty(request.Name))
            department.Name = request.Name.Trim();

        if (!string.IsNullOrEmpty(request.Code))
            department.Code = request.Code.Trim();

        if (!string.IsNullOrEmpty(request.Description))
            department.Description = request.Description.Trim();

        if (request.IsActive.HasValue)
            department.IsActive = request.IsActive.Value;

        // Updated ველები (თუ გაქვს ბაზაში)
        // department.UpdatedAt = DateTime.UtcNow;
        // department.UpdatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/departments/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
            return NotFound(new { message = "დეპარტამენტი არ მოიძებნა" });

        // შემოწმება: აქვს თუ არა დაკავშირებული აქტივები
        var hasAssets = await _context.Assets.AnyAsync(a => a.DepartmentId == id);
        if (hasAssets)
            return BadRequest(new { message = "დეპარტამენტს აქვს დაკავშირებული აქტივები. წაშლა შეუძლებელია." });

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}