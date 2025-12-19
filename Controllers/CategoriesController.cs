using AssetManagementApi.Data;
using AssetManagementApi.DTOs;
using AssetManagementApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AssetManagementApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/categories
[HttpGet]
[AllowAnonymous]
public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
{
    var categoriesFromDb = await _context.Categories
        .Where(c => c.IsActive)
        .Include(c => c.ParentCategory)  // ParentCategory-ის ჩატვირთვა
        .OrderBy(c => c.Name)
        .ToListAsync();

    var categories = categoriesFromDb.Select(c => new CategoryDto(
        c.Id,
        c.Name,
        c.Code,
        c.ParentCategoryId,
        c.ParentCategory?.Name,
        c.Description,
        c.IsActive,
        c.CreatedAt
    )).ToList();

    return Ok(categories);
}

// GET: api/categories/5
[HttpGet("{id}")]
[AllowAnonymous]
public async Task<ActionResult<CategoryDto>> GetCategory(int id)
{
    var categoryFromDb = await _context.Categories
        .Include(c => c.ParentCategory)
        .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

    if (categoryFromDb == null)
        return NotFound(new { message = "კატეგორია არ მოიძებნა" });

    var category = new CategoryDto(
        categoryFromDb.Id,
        categoryFromDb.Name,
        categoryFromDb.Code,
        categoryFromDb.ParentCategoryId,
        categoryFromDb.ParentCategory?.Name,
        categoryFromDb.Description,
        categoryFromDb.IsActive,
        categoryFromDb.CreatedAt
    );

    return Ok(category);
}

    // POST: api/categories
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CategoryCreateDto request)
    {
        // უნიკალურობის შემოწმება Code-ზე
        if (await _context.Categories.AnyAsync(c => c.Code == request.Code))
            return Conflict(new { message = "ეს კოდი უკვე გამოყენებულია" });

        // ParentCategory არსებობს?
        if (request.ParentCategoryId.HasValue && !await _context.Categories.AnyAsync(c => c.Id == request.ParentCategoryId))
            return BadRequest(new { message = "მშობელი კატეგორია არ მოიძებნა" });

        var newCategory = new Category
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            ParentCategoryId = request.ParentCategoryId,
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system"
        };

        _context.Categories.Add(newCategory);
        await _context.SaveChangesAsync();

        var dto = new CategoryDto(
            newCategory.Id,
            newCategory.Name,
            newCategory.Code,
            newCategory.ParentCategoryId,
            newCategory.ParentCategory?.Name,
            newCategory.Description,
            newCategory.IsActive,
            newCategory.CreatedAt
        );

        return CreatedAtAction(nameof(GetCategory), new { id = newCategory.Id }, dto);
    }

    // PUT: api/categories/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDto request)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound(new { message = "კატეგორია არ მოიძებნა" });

        // კოდის უნიკალურობა (გარდა საკუთარი თავისა)
        if (!string.IsNullOrEmpty(request.Code) && request.Code != category.Code)
        {
            if (await _context.Categories.AnyAsync(c => c.Code == request.Code))
                return Conflict(new { message = "ეს კოდი უკვე გამოყენებულია" });
        }

        if (!string.IsNullOrEmpty(request.Name))
            category.Name = request.Name.Trim();

        if (!string.IsNullOrEmpty(request.Code))
            category.Code = request.Code.Trim();

        if (request.ParentCategoryId.HasValue)
        {
            if (!await _context.Categories.AnyAsync(c => c.Id == request.ParentCategoryId))
                return BadRequest(new { message = "მშობელი კატეგორია არ მოიძებნა" });
            category.ParentCategoryId = request.ParentCategoryId;
        }

        if (!string.IsNullOrEmpty(request.Description))
            category.Description = request.Description.Trim();

        if (request.IsActive.HasValue)
            category.IsActive = request.IsActive.Value;

        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/categories/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound(new { message = "კატეგორია არ მოიძებნა" });

        // შემოწმება: აქვს თუ არა აქტივები ან ქვეკატეგორიები
        var hasAssets = await _context.Assets.AnyAsync(a => a.CategoryId == id);
        var hasSubCategories = await _context.Categories.AnyAsync(c => c.ParentCategoryId == id);

        if (hasAssets || hasSubCategories)
            return BadRequest(new { message = "კატეგორიას აქვს დაკავშირებული აქტივები ან ქვეკატეგორიები. წაშლა შეუძლებელია." });

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}