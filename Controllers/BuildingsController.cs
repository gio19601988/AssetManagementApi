using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using AssetManagementApi.DTOs;
using AssetManagementApi.Models;

namespace AssetManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuildingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BuildingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Buildings
        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<IEnumerable<BuildingDto>>> GetBuildings()
        {
            var buildings = await _context.Buildings
                .Select(b => new BuildingDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Code = b.Code,
                    Address = b.Address,
                    Notes = b.Notes,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    CreatedBy = b.CreatedBy
                })
                .OrderBy(b => b.Name)
                .ToListAsync();

            return Ok(buildings);
        }

        // GET: api/Buildings/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<BuildingDto>> GetBuilding(int id)
        {
            var building = await _context.Buildings
                .Where(b => b.Id == id)
                .Select(b => new BuildingDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Code = b.Code,
                    Address = b.Address,
                    Notes = b.Notes,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    CreatedBy = b.CreatedBy
                })
                .FirstOrDefaultAsync();

            if (building == null) return NotFound();
            return Ok(building);
        }

        // POST: api/Buildings
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BuildingDto>> CreateBuilding(CreateUpdateBuildingDto dto)
        {
            var building = new Building
            {
                Name = dto.Name,
                Code = dto.Code,
                Address = dto.Address,
                Notes = dto.Notes,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.Buildings.Add(building);
            await _context.SaveChangesAsync();

            var result = new BuildingDto
            {
                Id = building.Id,
                Name = building.Name,
                Code = building.Code,
                Address = building.Address,
                Notes = building.Notes,
                IsActive = building.IsActive,
                CreatedAt = building.CreatedAt,
                CreatedBy = building.CreatedBy
            };

            return CreatedAtAction(nameof(GetBuilding), new { id = building.Id }, result);
        }

        // PUT: api/Buildings/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBuilding(int id, CreateUpdateBuildingDto dto)
        {
            var building = await _context.Buildings.FindAsync(id);
            if (building == null) return NotFound();

            building.Name = dto.Name;
            building.Code = dto.Code;
            building.Address = dto.Address;
            building.Notes = dto.Notes;
            building.IsActive = dto.IsActive;
            building.UpdatedAt = DateTime.UtcNow;
            building.UpdatedBy = User.Identity?.Name ?? "System";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict();
            }

            return NoContent();
        }

        // DELETE: api/Buildings/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBuilding(int id)
        {
            var building = await _context.Buildings.FindAsync(id);
            if (building == null) return NotFound();

            _context.Buildings.Remove(building);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}