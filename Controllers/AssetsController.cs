using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AssetManagementApi.Repositories;
using AssetManagementApi.Models.DTO;

namespace AssetManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Basic Auth საჭიროა
public class AssetsController : ControllerBase
{
    private readonly AssetRepository _repository;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(AssetRepository repository, ILogger<AssetsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // GET: api/assets
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssets()
    {
        var assets = await _repository.GetAllAsync();
        return Ok(assets);
    }

    // GET: api/assets/5
    [HttpGet("{id}")]
    public async Task<ActionResult<AssetDto>> GetAsset(int id)
    {
        var asset = await _repository.GetByIdAsync(id);
        if (asset == null) return NotFound();
        return Ok(asset);
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
    // PUT: api/assets/5
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

    // DELETE: api/assets/5
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
}