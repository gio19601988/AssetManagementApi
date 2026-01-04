// Controllers/ImportController.cs
using Microsoft.EntityFrameworkCore;  // ← აუცილებელია AnyAsync, FirstOrDefaultAsync, Include-ისთვის
using AssetManagementApi.Data;
using AssetManagementApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Security.Claims;

namespace AssetManagementApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ImportController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ImportController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("assets")]
    public async Task<IActionResult> ImportAssets(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "ფაილი არ არის ატვირთული" });

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "მხოლოდ .xlsx ფაილია მიღებული" });

        var importedCount = 0;
        var errors = new List<string>();

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        using var package = new ExcelPackage(stream);

        var worksheet = package.Workbook.Worksheets[0];
        var rowCount = worksheet.Dimension.Rows;

        for (int row = 2; row <= rowCount; row++) // ვივარაუდებთ, რომ პირველი ხაზი header-ია
        {
            try
            {
                var assetName = worksheet.Cells[row, 1].GetValue<string>()?.Trim();
                if (string.IsNullOrEmpty(assetName))
                    continue;

                // შემოწმება უნიკალურობაზე (მაგ. Barcode ან SerialNumber)
                var barcode = worksheet.Cells[row, 2].GetValue<string>()?.Trim();
                var serialNumber = worksheet.Cells[row, 3].GetValue<string>()?.Trim();

                if (await _context.Assets.AnyAsync(a => a.Barcode == barcode || a.SerialNumber == serialNumber))
                {
                    errors.Add($"ხაზი {row}: უკვე არსებობს ბარკოდი/სერიული");
                    continue;
                }

                var asset = new Asset
                {
                    AssetName = assetName,
                    Barcode = barcode,
                    SerialNumber = serialNumber,
                    InventoryNumber = worksheet.Cells[row, 4].GetValue<string>()?.Trim(),
                    Manufacturer = worksheet.Cells[row, 5].GetValue<string>()?.Trim(),
                    PurchaseDate = worksheet.Cells[row, 6].GetValue<DateTime?>() ?? null,
                    PurchaseValue = worksheet.Cells[row, 7].GetValue<decimal?>() ?? 0,
                    CategoryId = await ResolveLookupId("Categories", worksheet.Cells[row, 8].GetValue<string>()),
                    DepartmentId = await ResolveLookupId("Departments", worksheet.Cells[row, 9].GetValue<string>()),
                    LocationId = await ResolveLookupId("Locations", worksheet.Cells[row, 10].GetValue<string>()),
                    AssetStatusId = await ResolveLookupId("AssetStatus", worksheet.Cells[row, 11].GetValue<string>()) ?? 1, // მაგ. "აქტიური"
                    ResponsiblePersonId = await ResolveLookupId("Employees", worksheet.Cells[row, 12].GetValue<string>()),
                    SupplierId = await ResolveLookupId("Suppliers", worksheet.Cells[row, 13].GetValue<string>()),
                    DepreciationMethodId = await ResolveLookupId("DepreciationMethods", worksheet.Cells[row, 14].GetValue<string>()) ?? 10, // Straight-Line
                    UsefulLifeMonths = worksheet.Cells[row, 15].GetValue<int?>(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "import"
                };

                _context.Assets.Add(asset);
                importedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"ხაზი {row}: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"იმპორტი დასრულდა: {importedCount} აქტივი დაემატა",
            errors = errors.Any() ? errors : null
        });
    }

    private async Task<int?> ResolveLookupId(string tableName, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        return tableName switch
        {
            "Categories" => await _context.Categories
                .Where(c => c.Name == name.Trim())
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync(),

            "Departments" => await _context.Departments
                .Where(d => d.Name == name.Trim())
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync(),

            "Locations" => await _context.Locations
                .Include(l => l.Building)
                .Where(l => $"{l.Building.Name} - {l.RoomNumber}" == name.Trim())
                .Select(l => (int?)l.Id)
                .FirstOrDefaultAsync(),

            "AssetStatus" => await _context.AssetStatus
                .Where(s => s.StatusName == name.Trim())  // ← აქ შეიცვალა Name → StatusName
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(),

            "Employees" => await _context.Employees
                .Where(e => e.FullName == name.Trim())
                .Select(e => (int?)e.Id)
                .FirstOrDefaultAsync(),

            "Suppliers" => await _context.Suppliers
                .Where(s => s.Name == name.Trim())
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(),

            "DepreciationMethods" => await _context.DepreciationMethods
                .Where(m => m.Name == name.Trim())
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync(),

            _ => null
        };
    }

    // სხვა იმპორტები (Categories, Locations, Employees, Suppliers) მსგავსად შეგიძლია დაამატო
}