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
public class LocationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LocationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/locations
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<LocationDto>>> GetLocations()
    {
        var locationsFromDb = await _context.Locations
            .Include(l => l.Building)
            .Where(l => l.IsActive)
            .OrderBy(l => l.Building.Name).ThenBy(l => l.RoomNumber)
            .ToListAsync();

        var locations = locationsFromDb.Select(l => new LocationDto(
            l.Id,
            $"{l.Building.Name} - {l.RoomNumber}",  // Name
            l.BuildingId,
            l.Building.Name,
            l.RoomNumber,
            l.IsActive,
            l.CreatedAt
        )).ToList();

        return Ok(locations);
    }

    // GET: api/locations/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<LocationDto>> GetLocation(int id)
    {
        var locationFromDb = await _context.Locations
            .Include(l => l.Building)
            .FirstOrDefaultAsync(l => l.Id == id && l.IsActive);

        if (locationFromDb == null)
            return NotFound(new { message = "ადგილმდებარეობა არ მოიძებნა" });

        var location = new LocationDto(
            locationFromDb.Id,
            $"{locationFromDb.Building.Name} - {locationFromDb.RoomNumber}",
            locationFromDb.BuildingId,
            locationFromDb.Building.Name,
            locationFromDb.RoomNumber,
            locationFromDb.IsActive,
            locationFromDb.CreatedAt
        );

        return Ok(location);
    }

    // POST: api/locations
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LocationDto>> CreateLocation([FromBody] LocationCreateDto request)
    {
        if (!await _context.Buildings.AnyAsync(b => b.Id == request.BuildingId))
            return BadRequest(new { message = "შენობა არ მოიძებნა" });

        var newLocation = new Location
        {
            BuildingId = request.BuildingId,
            RoomNumber = request.RoomNumber.Trim(),
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

    // PUT და DELETE — უცვლელი (კარგია)
    // ... (შენი კოდი)
}