using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using AssetManagementApi.DTOs;
using AssetManagementApi.Models.DTO;

namespace AssetManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StockController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StockController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/stock/current
[HttpGet("current")]
public async Task<ActionResult<IEnumerable<CurrentStockDto>>> GetCurrentStock(
    [FromQuery] int? warehouseId,
    [FromQuery] int? assetId)
{
    // მხოლოდ საჭირო მონაცემები — არანაირი Employee, Supplier
    var movements = await _context.StockMovements
        .Include(sm => sm.Asset)
        .Include(sm => sm.Warehouse)
        .Where(sm => 
            (warehouseId == null || sm.WarehouseId == warehouseId || sm.FromWarehouseId == warehouseId || sm.ToWarehouseId == warehouseId) &&
            (assetId == null || sm.AssetId == assetId))
        .Select(sm => new
        {
            sm.AssetId,
            sm.Asset.AssetName,
            sm.Asset.Barcode,
            sm.Asset.SerialNumber,
            sm.WarehouseId,
            sm.Warehouse.Name,
            sm.Warehouse.Level,
            sm.Quantity,
            sm.MovementType,
            sm.ToWarehouseId,
            sm.FromWarehouseId
        })
        .ToListAsync();

    if (!movements.Any())
        return Ok(new List<CurrentStockDto>());

    var grouped = movements
        .GroupBy(sm => new { sm.AssetId, WarehouseId = sm.WarehouseId })
        .Select(g => new CurrentStockDto
        {
            AssetId = g.Key.AssetId,
            AssetName = g.First().AssetName,
            Barcode = g.First().Barcode,
            SerialNumber = g.First().SerialNumber,
            WarehouseId = g.Key.WarehouseId,
            WarehouseName = g.First().Name,
            WarehouseLevel = g.First().Level,
            CurrentQuantity = g.Sum(sm =>
                sm.MovementType == "In" ? sm.Quantity :
                sm.MovementType == "Transfer" && sm.ToWarehouseId == g.Key.WarehouseId ? sm.Quantity :
                sm.MovementType == "Out" ? -sm.Quantity :
                sm.MovementType == "Transfer" && sm.FromWarehouseId == g.Key.WarehouseId ? -sm.Quantity : 0m
            )
        })
        .Where(x => x.CurrentQuantity > 0)
        .OrderBy(x => x.WarehouseName)
        .ThenBy(x => x.AssetName)
        .ToList();

    return Ok(grouped);
}
        // GET: api/stock/current/summary
        [HttpGet("current/summary")]
        public async Task<ActionResult<object>> GetCurrentStockSummary()
        {
            var movements = await _context.StockMovements.ToListAsync();

            var totalItems = movements.Select(sm => sm.AssetId).Distinct().Count();
            var totalQuantity = movements.Sum(sm =>
                sm.MovementType == "In" ? sm.Quantity :
                sm.MovementType == "Transfer" && sm.ToWarehouseId != null ? sm.Quantity :
                sm.MovementType == "Out" ? -sm.Quantity :
                sm.MovementType == "Transfer" && sm.FromWarehouseId != null ? -sm.Quantity : 0m
            );
            var warehousesWithStock = movements.Select(sm => sm.WarehouseId).Distinct().Count();

            return Ok(new
            {
                TotalItems = totalItems,
                TotalQuantity = totalQuantity,
                WarehousesWithStock = warehousesWithStock
            });
        }
    }
}