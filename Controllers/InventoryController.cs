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
                    ExpectedQuantity = 1  // შენ შეგიძლია შეცვალო ლოგიკა
                })
                .ToListAsync();

            return new { session, scans };
        }

        // POST: api/inventory/scan
        [HttpPost("scan")]
        public async Task<ActionResult> AddScan([FromBody] ScanRequest request)
        {
            // SessionId მოდის request-დან — არ გვჭირდება currentSessionId
            var session = await _context.InventorySessions.FindAsync(request.SessionId);
            if (session == null || session.Status != "Active")
                return BadRequest(new { message = "სესია არ მოიძებნა ან არ არის აქტიური" });

            var asset = await _context.Assets
                .Select(a => new { a.Id, a.Barcode, a.SerialNumber })
                .FirstOrDefaultAsync(a => a.Barcode == request.Barcode || a.SerialNumber == request.Barcode);

            if (asset == null)
                return NotFound(new { message = "აქტივი არ მოიძებნა ბარკოდით ან სერიული ნომრით" });

            var scan = new InventoryScan
            {
                SessionId = request.SessionId,
                AssetId = asset.Id,
                ScannedBarcode = request.Barcode,
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

        // GET: api/inventory/session/{id}/discrepancies
        [HttpGet("session/{id}/discrepancies")]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<object>> GetDiscrepancies(int id)
        {
            var session = await _context.InventorySessions.FindAsync(id);
            if (session == null) return NotFound("სესია არ მოიძებნა");

            // სკანირებული აქტივები (რაოდენობა)
            var scanned = await _context.InventoryScans
                .Where(s => s.SessionId == id)
                .GroupBy(s => s.AssetId)
                .Select(g => new
                {
                    AssetId = g.Key,
                    ScannedQuantity = g.Sum(s => s.Quantity)
                })
                .ToListAsync();

            var scannedAssetIds = scanned.Select(s => s.AssetId).ToHashSet();

            // ყველა აქტივი (ვივარაუდოთ, რომ რაოდენობა 1-ია)
            var allAssets = await _context.Assets
                .Select(a => new { a.Id, a.AssetName, a.Barcode, a.SerialNumber })
                .ToListAsync();

            // აკლია (არა სკანირებული)
            var missing = allAssets
                .Where(a => !scannedAssetIds.Contains(a.Id))
                .Select(a => new
                {
                    AssetId = a.Id,
                    AssetName = a.AssetName,
                    Barcode = a.Barcode ?? a.SerialNumber ?? "-",
                    Expected = 1,
                    Scanned = 0,
                    Difference = -1
                })
                .ToList();

            // ზედმეტია (მრავალჯერ სკანირებული)
            var excess = scanned
                .Where(s => s.ScannedQuantity > 1)
                .Join(allAssets,
                    s => s.AssetId,
                    a => a.Id,
                    (s, a) => new
                    {
                        AssetId = a.Id,
                        AssetName = a.AssetName,
                        Barcode = a.Barcode ?? a.SerialNumber ?? "-",
                        Expected = 1,
                        Scanned = s.ScannedQuantity,
                        Difference = s.ScannedQuantity - 1
                    })
                .ToList();

            // შესაბამისობა (სწორად სკანირებული)
            var matched = scanned
                .Where(s => s.ScannedQuantity == 1)
                .Join(allAssets,
                    s => s.AssetId,
                    a => a.Id,
                    (s, a) => new
                    {
                        AssetId = a.Id,
                        AssetName = a.AssetName,
                        Barcode = a.Barcode ?? a.SerialNumber ?? "-",
                        Expected = 1,
                        Scanned = 1,
                        Difference = 0
                    })
                .ToList();

            return Ok(new
            {
                session = new { session.Id, session.SessionName, session.Status },
                summary = new
                {
                    TotalAssets = allAssets.Count,
                    Scanned = scanned.Sum(s => s.ScannedQuantity),
                    Missing = missing.Count,
                    Excess = excess.Sum(e => e.Difference),
                    Matched = matched.Count
                },
                missing,
                excess,
                matched
            });
        }

        [HttpPost("session/{id}/reopen")]
        public async Task<IActionResult> ReopenSession(int id)
        {
            var session = await _context.InventorySessions.FindAsync(id);
            if (session == null) return NotFound();

            if (session.Status != "Completed") return BadRequest("სესია უკვე აქტიურია");

            session.Status = "Active";
            session.EndedAt = null; // ან გაანახლე
            await _context.SaveChangesAsync();

            return Ok(new { message: "სესია თავიდან გაიხსნა" });
        }

    }
}