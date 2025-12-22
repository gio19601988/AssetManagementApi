using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using AssetManagementApi.Models;
using AssetManagementApi.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace AssetManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,User")]
    public class InventoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/inventory/sessions
        [HttpGet("sessions")]
        public async Task<ActionResult<IEnumerable<InventorySession>>> GetSessions()
        {
            return await _context.InventorySessions
                .OrderByDescending(s => s.StartedAt)
                .ToListAsync();
        }

        // POST: api/inventory/session
        [HttpPost("session")]
        public async Task<ActionResult<InventorySession>> CreateSession([FromBody] CreateSessionRequest request)
        {
            var session = new InventorySession
            {
                SessionName = request.SessionName,
                DepartmentId = request.DepartmentId,  // ახალი: NULL თუ სრული, ID თუ კონკრეტული დეპარტამენტი
                CreatedBy = User.Identity?.Name ?? "Unknown",
                Status = "Active"
            };
            _context.InventorySessions.Add(session);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, session);
        }

        // GET: api/inventory/session/{id}
        [HttpGet("session/{id}")]
        public async Task<ActionResult<object>> GetSession(int id)
        {
            var session = await _context.InventorySessions.FindAsync(id);
            if (session == null) return NotFound();

            var scans = await _context.InventoryScans
                .Where(s => s.SessionId == id)
                .Include(s => s.Asset)
                .GroupBy(s => s.AssetId)
                .Select(g => new
                {
                    AssetId = g.Key,
                    AssetName = g.First().Asset.AssetName,
                    Barcode = g.First().Asset.Barcode,
                    ScannedQuantity = g.Sum(s => s.Quantity),
                    ExpectedQuantity = 1 // შეცვალე შენი ლოგიკით
                })
                .ToListAsync();

            return new { session, scans };
        }

        // POST: api/inventory/scan
        [HttpPost("scan")]
        public async Task<ActionResult> AddScan([FromBody] ScanRequest request)
        {
            var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Barcode == request.Barcode || a.SerialNumber == request.Barcode);
            if (asset == null) return NotFound(new { message = "აქტივი არ მოიძებნა ბარკოდით/სერიულით" });

            var scan = new InventoryScan
            {
                SessionId = request.SessionId,
                AssetId = asset.Id,
                ScannedBarcode = request.Barcode,
                ScannedAt = DateTime.UtcNow,
                ScannedBy = User.Identity?.Name ?? "Unknown",
                Quantity = 1
            };
            _context.InventoryScans.Add(scan);
            await _context.SaveChangesAsync();
            return Ok(new { message = "სკანი წარმატებით დარეგისტრირდა" });
        }

        // POST: api/inventory/session/{id}/complete
        [HttpPost("session/{id}/complete")]
        public async Task<IActionResult> CompleteSession(int id)
        {
            var session = await _context.InventorySessions.FindAsync(id);
            if (session == null) return NotFound();

            session.Status = "Completed";
            session.EndedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "სესია დასრულდა" });
        }

        // დისკრეპანსიის გენერაცია დეპარტამენტის მიხედვით
        // GET: api/inventory/session/{id}/discrepancies
        [HttpGet("session/{id}/discrepancies")]
        public async Task<ActionResult<object>> GetDiscrepancies(int id)
        {
            var session = await _context.InventorySessions.FindAsync(id);
            if (session == null) return NotFound();

            var expectedAssetsQuery = _context.Assets.AsQueryable();

            if (session.DepartmentId.HasValue)
            {
                expectedAssetsQuery = expectedAssetsQuery.Where(a => a.DepartmentId == session.DepartmentId.Value);
            }

            var expectedAssets = await expectedAssetsQuery.Select(a => new
            {
                a.Id,
                a.AssetName,
                a.Barcode,
                Expected = 1  // ან შენი ლოგიკა (მაგ. Quantity თუ გაქვს)
            }).ToListAsync();

            var scannedAssets = await _context.InventoryScans
                .Where(s => s.SessionId == id)
                .GroupBy(s => s.AssetId)
                .Select(g => new
                {
                    AssetId = g.Key,
                    Scanned = g.Count()
                }).ToListAsync();

            // შედარება და დისკრეპანსია
            // მაგალითად:
            var missing = expectedAssets.Where(e => !scannedAssets.Any(s => s.AssetId == e.Id)).ToList();
            var excess = scannedAssets.Where(s => !expectedAssets.Any(e => e.Id == s.AssetId)).ToList();
            var matched = scannedAssets.Where(s => expectedAssets.Any(e => e.Id == s.AssetId)).ToList();

            var summary = new {
                totalAssets = expectedAssets.Count,
                scanned = scannedAssets.Sum(s => s.Scanned),
                missing = missing.Count,
                excess = excess.Count,
                matched = matched.Count
            };

            return Ok(new { summary, missing, excess, matched });
        }
    }
}

public class CreateSessionRequest
{
    public string SessionName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }  // ახალი: optional დეპარტამენტი
}