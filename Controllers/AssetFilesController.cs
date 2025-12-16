using AssetManagementApi.Data;
using AssetManagementApi.DTOs;
using AssetManagementApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;  // ToListAsync-ისთვის

namespace AssetManagementApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AssetFilesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly string _uploadPath;

    public AssetFilesController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
        _uploadPath = Path.Combine(_env.WebRootPath, "files", "assets");
        Directory.CreateDirectory(_uploadPath);
    }

    // POST api/AssetFiles/upload
    [HttpPost("upload")]
    public async Task<ActionResult<AssetFileDto>> Upload([FromForm] FileUploadRequest request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest("ფაილი არ არის");

        var asset = await _context.Assets.FindAsync(request.AssetId);
        if (asset == null)
            return NotFound("აქტივი არ მოიძებნა");

        var uniqueName = Guid.NewGuid() + Path.GetExtension(request.File.FileName);
        var filePath = Path.Combine(_uploadPath, uniqueName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.File.CopyToAsync(stream);
        }

        var assetFile = new AssetFile
        {
            AssetId = request.AssetId,
            FileName = request.File.FileName,
            FileUrl = $"/files/assets/{uniqueName}",
            FileType = request.File.ContentType,
            FileCategory = request.FileCategory,
            UploadDate = DateTime.UtcNow
        };

        _context.AssetFiles.Add(assetFile);
        await _context.SaveChangesAsync();

        return Ok(new AssetFileDto(
            assetFile.Id,
            assetFile.AssetId,
            assetFile.FileName,
            assetFile.FileUrl,
            assetFile.FileType,
            assetFile.FileCategory,
            assetFile.UploadDate
        ));
    }

    // GET api/AssetFiles/asset/{assetId}
    [HttpGet("asset/{assetId}")]
    public async Task<ActionResult<IEnumerable<AssetFileDto>>> GetByAsset(int assetId)
    {
        var filesFromDb = await _context.AssetFiles
            .Where(f => f.AssetId == assetId)
            .OrderByDescending(f => f.UploadDate)
            .ToListAsync();

        var files = filesFromDb.Select(f => new AssetFileDto(
            f.Id,
            f.AssetId,
            f.FileName,
            f.FileUrl,
            f.FileType,
            f.FileCategory,
            f.UploadDate
        )).ToList();

        return Ok(files);
    }

    // DELETE api/AssetFiles/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var file = await _context.AssetFiles.FindAsync(id);
        if (file == null) return NotFound();

        var fullPath = Path.Combine(_env.WebRootPath, file.FileUrl.TrimStart('/'));
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);

        _context.AssetFiles.Remove(file);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}