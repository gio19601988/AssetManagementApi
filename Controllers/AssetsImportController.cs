// Controllers/AssetsImportController.cs
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using AssetManagementApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Security.Claims;

namespace AssetManagementApi.Controllers
{
    [Route("api/assets/import")]  // სწორი და ლოგიკური route
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AssetsImportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AssetsImportController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Excel ფაილიდან აქტივების მასობრივი იმპორტი
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ImportAssets(IFormFile file)
        {
            // ფაილის ვალიდაცია
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "ფაილი არ არის ატვირთული" });

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "მხოლოდ .xlsx ფორმატის ფაილია მიღებული" });

            var importedCount = 0;
            var errors = new List<string>();

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                using var package = new ExcelPackage(stream);

                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                if (rowCount < 2)
                    return BadRequest(new { message = "Excel ფაილში არ არის მონაცემები" });

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var assetName = worksheet.Cells[row, 1].GetValue<string>()?.Trim();
                        if (string.IsNullOrEmpty(assetName))
                            continue;

                        var barcode = worksheet.Cells[row, 2].GetValue<string>()?.Trim();
                        var serialNumber = worksheet.Cells[row, 3].GetValue<string>()?.Trim();

                        // უნიკალურობის შემოწმება
                        if (!string.IsNullOrEmpty(barcode) && await _context.Assets.AnyAsync(a => a.Barcode == barcode))
                        {
                            errors.Add($"ხაზი {row}: უკვე არსებობს ბარკოდი '{barcode}'");
                            continue;
                        }
                        if (!string.IsNullOrEmpty(serialNumber) && await _context.Assets.AnyAsync(a => a.SerialNumber == serialNumber))
                        {
                            errors.Add($"ხაზი {row}: უკვე არსებობს სერიული ნომერი '{serialNumber}'");
                            continue;
                        }

                        var asset = new Asset
                        {
                            AssetName = assetName,
                            Barcode = barcode,
                            SerialNumber = serialNumber,
                            InventoryNumber = worksheet.Cells[row, 4].GetValue<string>()?.Trim(),
                            Manufacturer = worksheet.Cells[row, 5].GetValue<string>()?.Trim(),
                            PurchaseDate = worksheet.Cells[row, 6].GetValue<DateTime?>(),
                            PurchaseValue = worksheet.Cells[row, 7].GetValue<decimal?>(),
                            CategoryId = await ResolveLookupId("Categories", worksheet.Cells[row, 8].GetValue<string>()),
                            DepartmentId = await ResolveLookupId("Departments", worksheet.Cells[row, 9].GetValue<string>()),
                            LocationId = await ResolveLookupId("Locations", worksheet.Cells[row, 10].GetValue<string>()),
                            AssetStatusId = await ResolveLookupId("AssetStatuses", worksheet.Cells[row, 11].GetValue<string>()) ?? 1,
                            ResponsiblePersonId = await ResolveLookupId("Employees", worksheet.Cells[row, 12].GetValue<string>()),
                            SupplierId = await ResolveLookupId("Suppliers", worksheet.Cells[row, 13].GetValue<string>()),
                            DepreciationMethodId = await ResolveLookupId("DepreciationMethods", worksheet.Cells[row, 14].GetValue<string>()) ?? 10,
                            UsefulLifeMonths = worksheet.Cells[row, 15].GetValue<int?>(),
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "import"
                        };

                        _context.Assets.Add(asset);
                        importedCount++;
                    }
                    catch (Exception rowEx)  // ✅ 'ex' → 'rowEx'
                    {
                        errors.Add($"ხაზი {row}: {rowEx.Message}");
                        
                        // დეტალური ინფორმაცია თუ პასუხისმგებელი არ მოიძებნა
                        if (rowEx.Message.Contains("ResponsiblePerson") || rowEx.Message.Contains("Employee"))
                        {
                            var responsibleName = worksheet.Cells[row, 12].GetValue<string>();
                            errors.Add($"  → პასუხისმგებელი პირი '{responsibleName}' არ მოიძებნა");
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "სერვერული შეცდომა იმპორტის დროს", detail = ex.Message });
            }

            return Ok(new
            {
                message = $"იმპორტი წარმატებით დასრულდა: {importedCount} აქტივი დაემატა",
                importedCount = importedCount,
                errors = errors.Any() ? errors : null
            });
        }

        /// <summary>
        /// Lookup ID-ის მოძებნა სახელის მიხედვით სხვადასხვა ცხრილიდან
        /// </summary>
        private async Task<int?> ResolveLookupId(string tableName, string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var trimmed = name.Trim();

            return tableName switch
            {
                "Categories" => await _context.Categories
                    .Where(c => c.Name == trimmed)
                    .Select(c => (int?)c.Id)
                    .FirstOrDefaultAsync(),

                "Departments" => await _context.Departments
                    .Where(d => d.Name == trimmed)
                    .Select(d => (int?)d.Id)
                    .FirstOrDefaultAsync(),

                "Locations" => await _context.Locations
    .Include(l => l.Building)
    .Where(l => l.RoomNumber == trimmed ||
                (l.Building != null && l.Building.Name != null &&
                 (l.Building.Name + " - " + l.RoomNumber) == trimmed))
    .Select(l => (int?)l.Id)
    .FirstOrDefaultAsync(),

                "AssetStatuses" => await _context.AssetStatus
                    .Where(s => s.StatusName == trimmed)
                    .Select(s => (int?)s.Id)
                    .FirstOrDefaultAsync(),

                "Employees" => await _context.Employees
                    .Where(e => e.FullName == trimmed)
                    .Select(e => (int?)e.Id)
                    .FirstOrDefaultAsync(),

                "Suppliers" => await _context.Suppliers
                    .Where(s => s.Name == trimmed || s.Code == trimmed)  // Code-ით ძებნა
                    .Select(s => (int?)s.Id)
                    .FirstOrDefaultAsync(),

                "DepreciationMethods" => await _context.DepreciationMethods
                    .Where(m => m.Name == trimmed)
                    .Select(m => (int?)m.Id)
                    .FirstOrDefaultAsync(),

                _ => null
            };
        }
    }
}