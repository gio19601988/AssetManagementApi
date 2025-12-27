using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;  // ← აუცილებელი ExecuteSqlRawAsync-ისთვის
using AssetManagementApi.Repositories;
using AssetManagementApi.Models.DTO;
using AssetManagementApi.Data;  // ← აქ არის ApplicationDbContext
using ZXing;
using ZXing.Common;
using SkiaSharp;
using ZXing.SkiaSharp;
using AssetManagementApi.DTOs;

namespace AssetManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly AssetRepository _repository;
    private readonly ApplicationDbContext _context;  // ← ახლა გამოყენებადია
    private readonly ILogger<AssetsController> _logger;

    // კონსტრუქტორში დაამატე ApplicationDbContext
    public AssetsController(AssetRepository repository, ApplicationDbContext context, ILogger<AssetsController> logger)
    {
        _repository = repository;
        _context = context;
        _logger = logger;
    }

    // GET: api/assets
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssets()
    {
        var assets = await _repository.GetAllAsync();
        return Ok(assets);
    }

    // GET: api/assets/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<AssetDto>> GetAsset(int id)
    {
        var asset = await _repository.GetByIdAsync(id);
        if (asset == null) return NotFound();
        return Ok(asset);
    }

    // GET: api/assets/lookup
    [HttpGet("lookup")]
    public async Task<IActionResult> GetAssetsLookup()
    {
        var assets = await _repository.GetAssetsLookupAsync();
        return Ok(assets);
    }

    // POST: api/assets
    [HttpPost]
    public async Task<ActionResult<AssetDto>> CreateAsset([FromBody] AssetCreateDto createDto)
    {
        _logger.LogInformation("Received asset create request: {@Request}", createDto);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState invalid: {Errors}", ModelState);
            return BadRequest(ModelState);
        }

        try
        {
            var userName = User.Identity?.Name ?? "system";
            var newId = await _repository.CreateAsync(createDto, userName);
            var newAsset = await _repository.GetByIdAsync(newId);

            return CreatedAtAction(nameof(GetAsset), new { id = newId }, newAsset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating asset");
            return BadRequest("Error creating asset: " + ex.Message);
        }
    }

    // PUT: api/assets/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAsset(int id, AssetUpdateDto updateDto)
    {
        if (id != updateDto.Id) return BadRequest("ID mismatch");

        try
        {
            var userName = User.Identity?.Name ?? "system";
            await _repository.UpdateAsync(updateDto, userName);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating asset {Id}", id);
            return BadRequest("Error updating asset: " + ex.Message);
        }
    }

    // DELETE: api/assets/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsset(int id)
    {
        try
        {
            var userName = User.Identity?.Name ?? "system";
            await _repository.DeleteAsync(id, userName);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset {Id}", id);
            return BadRequest("Error deleting asset: " + ex.Message);
        }
    }

    // POST: api/assets/run-depreciation
    [HttpPost("run-depreciation")]
    public async Task<ActionResult<string>> RunDepreciation()
    {
        try
        {
            var userName = User.Identity?.Name ?? "system";
            var result = await _repository.RunManualDepreciationAsync(userName);
            return Ok(new { message = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running depreciation");
            return BadRequest("Error running depreciation: " + ex.Message);
        }
    }

    // POST: api/assets/{id}/depreciate
    [HttpPost("{id}/depreciate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<string>> DepreciateSingleAsset(int id, [FromBody] SingleDepreciationRequest request)
    {
        try
        {
            var userName = User.Identity?.Name ?? "system";

            var sql = "EXEC usp_CalculateAssetDepreciation_Manual_V3 @Year = {0}, @Month = {1}, @AssetId = {2}, @RunBy = {3}";
            var parameters = new object[]
            {
            request.Year ?? (object)DBNull.Value,
            request.Month ?? (object)DBNull.Value,
            id,
            userName
            };

            await _context.Database.ExecuteSqlRawAsync(sql, parameters);

            return Ok($"ამორტიზაცია წარმატებით დარიცხა აქტივზე ID: {id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error depreciating single asset {Id}", id);
            return BadRequest("შეცდომა: " + ex.Message);
        }
    }

    // GET: api/assets/{id}/barcode
    [HttpGet("{id}/barcode")]
    public async Task<IActionResult> GetBarcode(int id)
    {
        var asset = await _repository.GetByIdAsync(id);
        if (asset == null)
            return NotFound();

        var barcodeText = !string.IsNullOrEmpty(asset.Barcode)
            ? asset.Barcode
            : asset.InventoryNumber ?? asset.Id.ToString("D10");

        var writer = new BarcodeWriter<SKBitmap>
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Height = 100,
                Width = 300,
                Margin = 10,
                PureBarcode = false
            }
        };

        using var bitmap = writer.Write(barcodeText);
        using var stream = new MemoryStream();
        bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        var bytes = stream.ToArray();

        return File(bytes, "image/png", $"Barcode_Asset_{id}.png");
    }

    // GET: api/assets/generate-barcode
    [HttpGet("generate-barcode")]
    public IActionResult GenerateBarcode()
    {
        var random = new Random();
        var barcode = random.Next(10000000, 99999999).ToString();

        return Ok(new { barcode });
    }
}