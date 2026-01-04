// Controllers/LocationsController.cs
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
[Authorize(Roles = "Admin")]  // მხოლოდ ადმინი მართავს
public class LocationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LocationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/locations — სრული სია
    [HttpGet]
    [AllowAnonymous]  // ყველას შეუძლია წაიკითხოს (ფორმებში გამოყენებისთვის)
    public async Task<ActionResult<IEnumerable<LocationDto>>> GetLocations()
    {
        var locations = await _context.Locations
            .Include(l => l.Building)
            .Where(l => l.IsActive)
            .OrderBy(l => l.Building.Name)
            .ThenBy(l => l.RoomNumber)
            .Select(l => new LocationDto(
                l.Id,
                $"{l.Building.Name} - {l.RoomNumber}",
                l.BuildingId,
                l.Building.Name,
                l.RoomNumber,
                l.IsActive,
                l.CreatedAt
            ))
            .ToListAsync();

        return Ok(locations);
    }

    // GET: api/locations/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<LocationDto>> GetLocation(int id)
    {
        var location = await _context.Locations
            .Include(l => l.Building)
            .Where(l => l.Id == id && l.IsActive)
            .Select(l => new LocationDto(
                l.Id,
                $"{l.Building.Name} - {l.RoomNumber}",
                l.BuildingId,
                l.Building.Name,
                l.RoomNumber,
                l.IsActive,
                l.CreatedAt
            ))
            .FirstOrDefaultAsync();

        if (location == null)
            return NotFound(new { message = "ადგილმდებარეობა არ მოიძებნა" });

        return Ok(location);
    }

    // POST: api/locations — ახლის დამატება
    [HttpPost]
    public async Task<ActionResult<LocationDto>> CreateLocation([FromBody] LocationCreateDto request)
    {
        if (!await _context.Buildings.AnyAsync(b => b.Id == request.BuildingId && b.IsActive))
            return BadRequest(new { message = "შენობა არ მოიძებნა ან არააქტიურია" });

        // უნიკალურობის შემოწმება (ერთ შენობაში ერთი და იგივე ოთახის ნომერი)
        if (await _context.Locations.AnyAsync(l => 
            l.BuildingId == request.BuildingId && 
            l.RoomNumber == request.RoomNumber.Trim()))
            return Conflict(new { message = "ეს ოთახის ნომერი უკვე არსებობს ამ შენობაში" });

        var newLocation = new Location
        {
            BuildingId = request.BuildingId,
            RoomNumber = request.RoomNumber.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system"
        };

        _context.Locations.Add(newLocation);
        await _context.SaveChangesAsync();

        await _context.Entry(newLocation).Reference(l => l.Building).LoadAsync();

        var dto = new LocationDto(
            newLocation.Id,
            $"{newLocation.Building.Name} - {newLocation.RoomNumber}",
            newLocation.BuildingId,
            newLocation.Building.Name,
            newLocation.RoomNumber,
            newLocation.IsActive,
            newLocation.CreatedAt
        );

        return CreatedAtAction(nameof(GetLocation), new { id = newLocation.Id }, dto);
    }

    // PUT: api/locations/{id} — განახლება
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLocation(int id, [FromBody] LocationUpdateDto request)
    {
        var location = await _context.Locations
            .Include(l => l.Building)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (location == null)
            return NotFound(new { message = "ადგილმდებარეობა არ მოიძებნა" });

        // უნიკალურობა (გარდა საკუთარი თავისა)
        if (await _context.Locations.AnyAsync(l => 
            l.Id != id &&
            l.BuildingId == request.BuildingId && 
            l.RoomNumber == request.RoomNumber.Trim()))
            return Conflict(new { message = "ეს ოთახის ნომერი უკვე არსებობს ამ შენობაში" });

        location.BuildingId = request.BuildingId;
        location.RoomNumber = request.RoomNumber.Trim();
        location.Description = request.Description?.Trim();
        location.IsActive = request.IsActive;
        location.UpdatedAt = DateTime.UtcNow;
        location.UpdatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/locations/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        var location = await _context.Locations.FindAsync(id);
        if (location == null)
            return NotFound(new { message = "ადგილმდებარეობა არ მოიძებნა" });

        // თუ აქტივებია მიბმული – აკრძალვა (ან გადატანა „უცნობ“ ლოკაციაზე)
        if (await _context.Assets.AnyAsync(a => a.LocationId == id))
            return BadRequest(new { message = "ამ ადგილმდებარეობაზე მიბმულია აქტივები – წაშლა შეუძლებელია" });

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}