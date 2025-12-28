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
    public class StockMovementsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StockMovementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/stockmovements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockMovementDto>>> GetStockMovements()
        {
            var movements = await _context.StockMovements
                .Include(sm => sm.Asset)
                .Include(sm => sm.Warehouse)
                .Include(sm => sm.FromWarehouse)
                .Include(sm => sm.ToWarehouse)
                .Include(sm => sm.Supplier)
                .Include(sm => sm.Employee)
                .Select(sm => new StockMovementDto
                {
                    Id = sm.Id,
                    AssetId = sm.AssetId,
                    AssetName = sm.Asset.AssetName,
                    WarehouseId = sm.WarehouseId,
                    WarehouseName = sm.Warehouse.Name,
                    Quantity = sm.Quantity,
                    MovementType = sm.MovementType,
                    FromWarehouseId = sm.FromWarehouseId,
                    FromWarehouseName = sm.FromWarehouse != null ? sm.FromWarehouse.Name : null,
                    ToWarehouseId = sm.ToWarehouseId,
                    ToWarehouseName = sm.ToWarehouse != null ? sm.ToWarehouse.Name : null,
                    SupplierId = sm.SupplierId,
                    SupplierName = sm.Supplier != null ? sm.Supplier.Name : null,
                    EmployeeId = sm.EmployeeId,
                    EmployeeName = sm.Employee != null ? sm.Employee.FullName : null,
                    ReferenceDocument = sm.ReferenceDocument,
                    MovementDate = sm.MovementDate,
                    Notes = sm.Notes,
                    CreatedAt = sm.CreatedAt,
                    CreatedBy = sm.CreatedBy
                })
                .OrderByDescending(sm => sm.CreatedAt)
                .ToListAsync();

            return Ok(movements);
        }

        // GET: api/stockmovements/{id} — აუცილებელი CreatedAtAction-ისთვის
        [HttpGet("{id}")]
        public async Task<ActionResult<StockMovementDto>> GetStockMovement(int id)
        {
            var movement = await _context.StockMovements
                .Include(sm => sm.Asset)
                .Include(sm => sm.Warehouse)
                .Include(sm => sm.FromWarehouse)
                .Include(sm => sm.ToWarehouse)
                .Include(sm => sm.Supplier)
                .Include(sm => sm.Employee)
                .Where(sm => sm.Id == id)
                .Select(sm => new StockMovementDto
                {
                    Id = sm.Id,
                    AssetId = sm.AssetId,
                    AssetName = sm.Asset.AssetName,
                    WarehouseId = sm.WarehouseId,
                    WarehouseName = sm.Warehouse.Name,
                    Quantity = sm.Quantity,
                    MovementType = sm.MovementType,
                    FromWarehouseId = sm.FromWarehouseId,
                    FromWarehouseName = sm.FromWarehouse != null ? sm.FromWarehouse.Name : null,
                    ToWarehouseId = sm.ToWarehouseId,
                    ToWarehouseName = sm.ToWarehouse != null ? sm.ToWarehouse.Name : null,
                    SupplierId = sm.SupplierId,
                    SupplierName = sm.Supplier != null ? sm.Supplier.Name : null,
                    EmployeeId = sm.EmployeeId,
                    EmployeeName = sm.Employee != null ? sm.Employee.FullName : null,
                    ReferenceDocument = sm.ReferenceDocument,
                    MovementDate = sm.MovementDate,
                    Notes = sm.Notes,
                    CreatedAt = sm.CreatedAt,
                    CreatedBy = sm.CreatedBy
                })
                .FirstOrDefaultAsync();

            if (movement == null) return NotFound();

            return Ok(movement);
        }

        // POST: api/stockmovements
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<StockMovementDto>> CreateStockMovement(CreateStockMovementDto dto)
        {
            var movement = new StockMovement
            {
                AssetId = dto.AssetId,
                WarehouseId = dto.WarehouseId,
                Quantity = dto.Quantity,
                MovementType = dto.MovementType,
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId,
                SupplierId = dto.SupplierId,
                EmployeeId = dto.EmployeeId,
                ReferenceDocument = dto.ReferenceDocument,
                MovementDate = dto.MovementDate ?? DateTime.UtcNow,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "system"
            };

            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();

            // ახლა GetStockMovement(id) არსებობს — CreatedAtAction იმუშავებს
            return CreatedAtAction(nameof(GetStockMovement), new { id = movement.Id }, movement);
        }
    }
}