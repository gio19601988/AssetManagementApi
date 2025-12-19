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
public class AssetStatusesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AssetStatusesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/assetstatuses
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<AssetStatusDto>>> GetAssetStatuses()
    {
        var statusesFromDb = await _context.AssetStatus
            .Where(s => s.IsActive)
            .OrderBy(s => s.StatusName)
            .ToListAsync();

        var statuses = statusesFromDb.Select(s => new AssetStatusDto(
    s.Id,
    s.StatusName,  // Name = StatusName
    s.Code,
    s.Description,
    s.IsActive,
    s.CreatedAt
)).ToList();

        return Ok(statuses);
    }

    // GET: api/assetstatuses/5
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<AssetStatusDto>> GetAssetStatus(int id)
    {
        var statusFromDb = await _context.AssetStatus
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

        if (statusFromDb == null)
            return NotFound(new { message = "აქტივის სტატუსი არ მოიძებნა" });

        var status = new AssetStatusDto(
            statusFromDb.Id,
            statusFromDb.StatusName,
            statusFromDb.Code,
            statusFromDb.Description,
            statusFromDb.IsActive,
            statusFromDb.CreatedAt
        );

        return Ok(status);
    }

    // POST: api/assetstatuses
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AssetStatusDto>> CreateAssetStatus([FromBody] AssetStatusCreateDto request)
    {
        if (!string.IsNullOrEmpty(request.Code))
        {
            if (await _context.AssetStatus.AnyAsync(s => s.Code == request.Code))
                return Conflict(new { message = "ეს კოდი უკვე გამოყენებულია" });
        }

        var newStatus = new AssetStatus
        {
            StatusName = request.StatusName.Trim(),
            Code = request.Code?.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system"
        };

        _context.AssetStatus.Add(newStatus);
        await _context.SaveChangesAsync();

        var dto = new AssetStatusDto(
            newStatus.Id,
            newStatus.StatusName,
            newStatus.Code,
            newStatus.Description,
            newStatus.IsActive,
            newStatus.CreatedAt
        );

        return CreatedAtAction(nameof(GetAssetStatus), new { id = newStatus.Id }, dto);
    }

    // PUT: api/assetstatuses/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAssetStatus(int id, [FromBody] AssetStatusUpdateDto request)
    {
        var status = await _context.AssetStatus.FindAsync(id);
        if (status == null)
            return NotFound(new { message = "აქტივის სტატუსი არ მოიძებნა" });

        if (!string.IsNullOrEmpty(request.Code) && request.Code != status.Code)
        {
            if (await _context.AssetStatus.AnyAsync(s => s.Code == request.Code))
                return Conflict(new { message = "ეს კოდი უკვე გამოყენებულია" });
        }

        if (!string.IsNullOrEmpty(request.StatusName))
            status.StatusName = request.StatusName.Trim();

        if (!string.IsNullOrEmpty(request.Code))
            status.Code = request.Code.Trim();

        if (!string.IsNullOrEmpty(request.Description))
            status.Description = request.Description.Trim();

        if (request.IsActive.HasValue)
            status.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/assetstatuses/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAssetStatus(int id)
    {
        var status = await _context.AssetStatus.FindAsync(id);
        if (status == null)
            return NotFound(new { message = "აქტივის სტატუსი არ მოიძებნა" });

        var hasAssets = await _context.Assets.AnyAsync(a => a.AssetStatusId == id);
        if (hasAssets)
            return BadRequest(new { message = "ეს სტატუსი გამოიყენება აქტივებში. წაშლა შეუძლებელია." });

        _context.AssetStatus.Remove(status);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}