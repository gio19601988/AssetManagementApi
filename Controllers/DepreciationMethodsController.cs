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
public class DepreciationMethodsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DepreciationMethodsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/depreciationmethods
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<DepreciationMethodDto>>> GetDepreciationMethods()
    {
        var methodsFromDb = await _context.DepreciationMethods
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync();

        var methods = methodsFromDb.Select(m => new DepreciationMethodDto(
            m.Id,
            m.Name,
            m.Code,
            m.Description,
            m.IsActive,
            m.CreatedAt
        )).ToList();

        return Ok(methods);
    }

    // GET: api/depreciationmethods/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<DepreciationMethodDto>> GetDepreciationMethod(int id)
    {
        var methodFromDb = await _context.DepreciationMethods
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (methodFromDb == null)
            return NotFound(new { message = "ამორტიზაციის მეთოდი არ მოიძებნა" });

        var method = new DepreciationMethodDto(
            methodFromDb.Id,
            methodFromDb.Name,
            methodFromDb.Code,
            methodFromDb.Description,
            methodFromDb.IsActive,
            methodFromDb.CreatedAt
        );

        return Ok(method);
    }

    // POST: api/depreciationmethods
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DepreciationMethodDto>> CreateDepreciationMethod([FromBody] DepreciationMethodCreateDto request)
    {
        if (await _context.DepreciationMethods.AnyAsync(m => m.Code == request.Code))
            return Conflict(new { message = "ეს კოდი უკვე გამოყენებულია" });

        var newMethod = new DepreciationMethod
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system"
        };

        _context.DepreciationMethods.Add(newMethod);
        await _context.SaveChangesAsync();

        var dto = new DepreciationMethodDto(
            newMethod.Id,
            newMethod.Name,
            newMethod.Code,
            newMethod.Description,
            newMethod.IsActive,
            newMethod.CreatedAt
        );

        return CreatedAtAction(nameof(GetDepreciationMethod), new { id = newMethod.Id }, dto);
    }

    // PUT: api/depreciationmethods/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDepreciationMethod(int id, [FromBody] DepreciationMethodUpdateDto request)
    {
        var method = await _context.DepreciationMethods.FindAsync(id);
        if (method == null)
            return NotFound(new { message = "ამორტიზაციის მეთოდი არ მოიძებნა" });

        if (!string.IsNullOrEmpty(request.Code) && request.Code != method.Code)
        {
            if (await _context.DepreciationMethods.AnyAsync(m => m.Code == request.Code))
                return Conflict(new { message = "ეს კოდი უკვე გამოყენებულია" });
        }

        if (!string.IsNullOrEmpty(request.Name))
            method.Name = request.Name.Trim();

        if (!string.IsNullOrEmpty(request.Code))
            method.Code = request.Code.Trim();

        if (!string.IsNullOrEmpty(request.Description))
            method.Description = request.Description.Trim();

        if (request.IsActive.HasValue)
            method.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/depreciationmethods/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDepreciationMethod(int id)
    {
        var method = await _context.DepreciationMethods.FindAsync(id);
        if (method == null)
            return NotFound(new { message = "ამორტიზაციის მეთოდი არ მოიძებნა" });

        var hasAssets = await _context.Assets.AnyAsync(a => a.DepreciationMethodId == id);
        if (hasAssets)
            return BadRequest(new { message = "ეს მეთოდი გამოიყენება აქტივებში. წაშლა შეუძლებელია." });

        _context.DepreciationMethods.Remove(method);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}