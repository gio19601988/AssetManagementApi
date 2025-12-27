using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using Microsoft.AspNetCore.Authorization;

namespace AssetManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,User")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/reports/assets
        // აქტივების სრული სია (შენს მოდელში არსებული ველებით)
        [HttpGet("assets")]
        public async Task<ActionResult<IEnumerable<object>>> GetAssetsReport()
        {
            var report = await _context.Assets
                .Select(a => new
                {
                    a.Id,
                    a.AssetName,
                    a.Manufacturer,
                    a.PurchaseValue,
                    a.PurchaseDate,
                    a.UsefulLifeMonths,
                    a.Currency,
                    a.Barcode,
                    a.SerialNumber,
                    a.InventoryNumber,
                    CategoryId = a.CategoryId,
                    DepartmentId = a.DepartmentId,
                    LocationId = a.LocationId,
                    StatusId = a.AssetStatusId,
                    ResponsiblePersonId = a.ResponsiblePersonId,
                    a.DisposalValue,
                    a.CreatedAt
                })
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            return Ok(report);
        }

        // GET: api/reports/depreciation
        [HttpGet("depreciation")]
        public async Task<ActionResult<IEnumerable<object>>> GetDepreciationReport()
        {
            var report = await _context.Assets
                .Where(a => a.DepreciationMethodId != null && a.UsefulLifeMonths.HasValue && a.UsefulLifeMonths > 0)
                .Select(a => new
                {
                    a.Id,
                    a.AssetName,
                    a.PurchaseValue,
                    a.SalvageValue,
                    a.UsefulLifeMonths,
                    a.DepreciationMethodId,
                    a.DepreciationStartDate,

                    MonthlyDepreciation = ((a.PurchaseValue ?? 0m) - (a.SalvageValue ?? 0m)) / a.UsefulLifeMonths.Value,

                    MonthsPassed = a.DepreciationStartDate.HasValue
                        ? (int)((DateTime.UtcNow - a.DepreciationStartDate.Value).TotalDays / 30)
                        : 0,

                    DepreciatedAmount = a.DepreciationStartDate.HasValue
                        ? Math.Min(
                            ((a.PurchaseValue ?? 0m) - (a.SalvageValue ?? 0m)) / a.UsefulLifeMonths.Value *
                            (int)((DateTime.UtcNow - a.DepreciationStartDate.Value).TotalDays / 30),
                            (a.PurchaseValue ?? 0m) - (a.SalvageValue ?? 0m)
                          )
                        : 0m,

                    CurrentValue = (a.PurchaseValue ?? 0m) -
                        (a.DepreciationStartDate.HasValue
                            ? Math.Min(
                                ((a.PurchaseValue ?? 0m) - (a.SalvageValue ?? 0m)) / a.UsefulLifeMonths.Value *
                                (int)((DateTime.UtcNow - a.DepreciationStartDate.Value).TotalDays / 30),
                                (a.PurchaseValue ?? 0m) - (a.SalvageValue ?? 0m)
                              )
                            : 0m)
                })
                .ToListAsync();

            return Ok(report);
        }

        // GET: api/reports/inventory
        // ინვენტარიზაციის სესიების შეჯამება
        [HttpGet("inventory")]
        public async Task<ActionResult<IEnumerable<object>>> GetInventoryReport()
        {
            var report = await _context.InventorySessions
                .Where(s => s.Status == "Completed")
                .Select(s => new
                {
                    s.Id,
                    s.SessionName,
                    StartedAt = s.StartedAt.ToString("yyyy-MM-dd"),
                    EndedAt = s.EndedAt.HasValue ? s.EndedAt.Value.ToString("yyyy-MM-dd") : null,
                    Department = s.DepartmentId.HasValue ? "კონკრეტული დეპარტამენტი" : "სრული კომპანია",
                    CreatedBy = s.CreatedBy
                })
                .OrderByDescending(s => s.StartedAt)
                .ToListAsync();

            return Ok(report);
        }

        // GET: api/reports/financial
        // ფინანსური შეჯამება
        [HttpGet("financial")]
        public async Task<ActionResult<object>> GetFinancialReport()
        {
            var totalPurchaseValue = await _context.Assets
                .SumAsync(a => (decimal?)a.PurchaseValue) ?? 0;

            var totalSalvageValue = await _context.Assets
                .SumAsync(a => (decimal?)a.SalvageValue) ?? 0;

            var totalDisposalValue = await _context.Assets
                .SumAsync(a => (decimal?)a.DisposalValue) ?? 0;

            var activeAssetsCount = await _context.Assets
                .CountAsync(a => a.AssetStatusId == 1);  // ვივარაუდებ, რომ 1 = Active

            return Ok(new
            {
                totalAssets = await _context.Assets.CountAsync(),
                activeAssetsCount,
                totalPurchaseValue,
                totalSalvageValue,
                totalDisposalValue,
                netBookValue = totalPurchaseValue - totalDisposalValue
            });
        }
    }
}