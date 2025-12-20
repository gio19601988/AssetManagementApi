using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AssetManagementApi.Data;
using AssetManagementApi.DTOs;  // ← შეცვალე შენი namespace-ით თუ სხვაა

namespace AssetManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepreciationHistoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DepreciationHistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/DepreciationHistory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DepreciationHistoryDto>>> GetDepreciationHistory()
        {
            var history = await _context.AssetDepreciationHistory
                .Include(h => h.Asset)
                .OrderByDescending(h => h.DepreciationDate)
                .ThenByDescending(h => h.CreatedAt)
                .Select(h => new DepreciationHistoryDto
                {
                    DepreciationID = h.DepreciationID,
                    AssetId = h.AssetId,
                    AssetName = h.Asset.AssetName,
                    DepreciationDate = h.DepreciationDate,
                    Amount = h.Amount,
                    DepreciationBook = h.DepreciationBook,
                    CreatedAt = h.CreatedAt,
                    CreatedBy = h.CreatedBy,
                    Notes = h.Notes
                })
                .ToListAsync();

            return Ok(history);
        }

        // GET: api/DepreciationHistory/asset/{assetId}
        [HttpGet("asset/{assetId}")]
        public async Task<ActionResult<IEnumerable<DepreciationHistoryDto>>> GetByAsset(int assetId)
        {
            var history = await _context.AssetDepreciationHistory
                .Where(h => h.AssetId == assetId)
                .Include(h => h.Asset)
                .OrderByDescending(h => h.DepreciationDate)
                .Select(h => new DepreciationHistoryDto
                {
                    DepreciationID = h.DepreciationID,
                    AssetId = h.AssetId,
                    AssetName = h.Asset.AssetName,
                    DepreciationDate = h.DepreciationDate,
                    Amount = h.Amount,
                    DepreciationBook = h.DepreciationBook,
                    CreatedAt = h.CreatedAt,
                    CreatedBy = h.CreatedBy,
                    Notes = h.Notes
                })
                .ToListAsync();

            return history.Any() ? Ok(history) : NotFound();
        }

        // POST: api/DepreciationHistory/delete-by-month
        [HttpPost("delete-by-month")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteByMonth([FromBody] DeleteByMonthRequest request)
        {
            try
            {
                var sql = "EXEC usp_DeleteDepreciationByMonth_V3 @Year = {0}, @Month = {1}";
                var parameters = new object[] { request.Year, request.Month };

                if (!string.IsNullOrEmpty(request.DepreciationBook))
                {
                    sql += ", @DepreciationBook = {2}";
                    parameters = new object[] { request.Year, request.Month, request.DepreciationBook };
                }
                else
                {
                    sql += ", @DepreciationBook = NULL";
                }

                await _context.Database.ExecuteSqlRawAsync(sql, parameters);

                return Ok(new { message = "დარიცხვა წარმატებით გაუქმდა თვის მიხედვით" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "შეცდომა წაშლისას: " + ex.Message });
            }
        }

        // POST: api/DepreciationHistory/delete-by-session
        [HttpPost("delete-by-session")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBySession([FromBody] DeleteBySessionRequest request)
        {
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC usp_DeleteDepreciationBySession_V3 @SessionID = {request.SessionID}");

                return Ok(new { message = "სესიის დარიცხვა წარმატებით გაუქმდა" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "შეცდომა წაშლისას: " + ex.Message });
            }
        }

        // POST: api/DepreciationHistory/delete-for-asset
        [HttpPost("delete-for-asset")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteForAsset([FromBody] DeleteForAssetRequest request)
        {
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC usp_DeleteDepreciationForAsset_V3 @AssetId = {request.AssetId}, @Year = {request.Year}, @Month = {request.Month}");

                return Ok(new { message = "აქტივის დარიცხვა წარმატებით გაუქმდა" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "შეცდომა წაშლისას: " + ex.Message });
            }
        }
    }
}