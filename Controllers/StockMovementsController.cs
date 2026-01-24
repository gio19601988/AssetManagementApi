// Controllers/StockMovementsController.cs
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

        /// <summary>
        /// ყველა მარაგის მოძრაობის მიღება DTO-ს სახით
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockMovementDto>>> GetStockMovements()
        {
            var movements = await _context.StockMovements
                .Include(sm => sm.Asset)
                .Include(sm => sm.Warehouse)
                .Include(sm => sm.FromWarehouse)
                .Include(sm => sm.ToWarehouse)
                .Include(sm => sm.Supplier)
                .Include(sm => sm.ResponsiblePerson)  // ← ახალი navigation (ResponsiblePerson)
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
                    ResponsiblePersonId = sm.ResponsiblePersonId,
                    ResponsiblePersonName = sm.ResponsiblePerson != null ? sm.ResponsiblePerson.FullName : null,  // ← გასწორებული!
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

        /// <summary>
        /// ერთი მოძრაობის მიღება ID-ით
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<StockMovementDto>> GetStockMovement(int id)
        {
            var movement = await _context.StockMovements
                .Include(sm => sm.Asset)
                .Include(sm => sm.Warehouse)
                .Include(sm => sm.FromWarehouse)
                .Include(sm => sm.ToWarehouse)
                .Include(sm => sm.Supplier)
                .Include(sm => sm.ResponsiblePerson)  // ← ახალი navigation
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
                    ResponsiblePersonId = sm.ResponsiblePersonId,
                    ResponsiblePersonName = sm.ResponsiblePerson != null ? sm.ResponsiblePerson.FullName : null,  // ← გასწორებული!
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

        /// <summary>
        /// ახალი მოძრაობის შექმნა ერთი ჩანაწერისთვის
        /// </summary>
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
                ResponsiblePersonId = dto.ResponsiblePersonId,  // ← სწორი ველი
                ReferenceDocument = dto.ReferenceDocument,
                MovementDate = dto.MovementDate ?? DateTime.UtcNow,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "system"
            };

            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStockMovement), new { id = movement.Id }, movement);
        }

        /// <summary>
        /// მრავალჯერადი მოძრაობის შექმნა (batch) — PDF-დან ან სხვა წყაროდან
        /// </summary>
        [HttpPost("batch")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBatch([FromBody] List<CreateStockMovementDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return BadRequest(new { message = "მონაცემები არ არის მიწოდებული" });

            var movements = new List<StockMovement>();

            foreach (var dto in dtos)
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
                    ResponsiblePersonId = dto.ResponsiblePersonId,
                    ReferenceDocument = dto.ReferenceDocument,
                    MovementDate = dto.MovementDate ?? DateTime.UtcNow,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "system"
                };

                movements.Add(movement);
                _context.StockMovements.Add(movement);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"{movements.Count} მოძრაობა წარმატებით დაემატა" });
        }
    }
}