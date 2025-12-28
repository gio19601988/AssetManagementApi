using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using AssetManagementApi.Models;
using AssetManagementApi.DTOs;

namespace AssetManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WarehouseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WarehouseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/warehouse
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WarehouseDto>>> GetWarehouses()
        {
            var warehouses = await _context.Warehouses
                .Include(w => w.Location)
                .Include(w => w.Department)
                .Include(w => w.ResponsiblePerson)
                .Select(w => new WarehouseDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Code = w.Code,
                    Level = w.Level,
                    LocationId = w.LocationId,
                    LocationName = w.Location != null ? w.Location.Name : null,
                    DepartmentId = w.DepartmentId,
                    DepartmentName = w.Department != null ? w.Department.Name : null,
                    ResponsiblePersonId = w.ResponsiblePersonId,
                    ResponsiblePersonName = w.ResponsiblePerson != null ? w.ResponsiblePerson.FullName : null,
                    Notes = w.Notes,
                    IsActive = w.IsActive,
                    CreatedAt = w.CreatedAt,
                    CreatedBy = w.CreatedBy
                })
                .OrderBy(w => w.Name)
                .ToListAsync();

            return Ok(warehouses);
        }

        // GET: api/warehouse/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<WarehouseDto>> GetWarehouse(int id)
        {
            var warehouse = await _context.Warehouses
                .Include(w => w.Location)
                .Include(w => w.Department)
                .Include(w => w.ResponsiblePerson)
                .Where(w => w.Id == id)
                .Select(w => new WarehouseDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Code = w.Code,
                    Level = w.Level,
                    LocationId = w.LocationId,
                    LocationName = w.Location != null ? w.Location.Name : null,
                    DepartmentId = w.DepartmentId,
                    DepartmentName = w.Department != null ? w.Department.Name : null,
                    ResponsiblePersonId = w.ResponsiblePersonId,
                    ResponsiblePersonName = w.ResponsiblePerson != null ? w.ResponsiblePerson.FullName : null,
                    Notes = w.Notes,
                    IsActive = w.IsActive,
                    CreatedAt = w.CreatedAt,
                    CreatedBy = w.CreatedBy
                })
                .FirstOrDefaultAsync();

            if (warehouse == null) return NotFound();
            return Ok(warehouse);
        }

        // POST: api/warehouse
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<WarehouseDto>> CreateWarehouse(CreateUpdateWarehouseDto dto)
        {
            var warehouse = new Warehouse
            {
                Name = dto.Name,
                Code = dto.Code,
                Level = dto.Level,
                LocationId = dto.LocationId,
                DepartmentId = dto.DepartmentId,
                ResponsiblePersonId = dto.ResponsiblePersonId,
                Notes = dto.Notes,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "system"
            };

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            var result = await GetWarehouse(warehouse.Id);
            return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, result.Value);
        }

        // PUT: api/warehouse/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateWarehouse(int id, CreateUpdateWarehouseDto dto)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null) return NotFound();

            warehouse.Name = dto.Name;
            warehouse.Code = dto.Code;
            warehouse.Level = dto.Level;
            warehouse.LocationId = dto.LocationId;
            warehouse.DepartmentId = dto.DepartmentId;
            warehouse.ResponsiblePersonId = dto.ResponsiblePersonId;
            warehouse.Notes = dto.Notes;
            warehouse.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/warehouse/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null) return NotFound();

            _context.Warehouses.Remove(warehouse);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}