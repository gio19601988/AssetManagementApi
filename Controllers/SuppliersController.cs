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
public class SuppliersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SuppliersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/suppliers
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetSuppliers()
    {
        var suppliersFromDb = await _context.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var suppliers = suppliersFromDb.Select(s => new SupplierDto(
            s.Id,
            s.Name,
            s.ContactPerson,
            s.Phone,
            s.Email,
            s.Address,
            s.Notes,
            s.IsActive,
            s.CreatedAt,
            s.Code
        )).ToList();

        return Ok(suppliers);
    }

    // GET: api/suppliers/5
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<SupplierDto>> GetSupplier(int id)
    {
        var supplierFromDb = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

        if (supplierFromDb == null)
            return NotFound(new { message = "მომწოდებელი არ მოიძებნა" });

        var supplier = new SupplierDto(
            supplierFromDb.Id,
            supplierFromDb.Name,
            supplierFromDb.ContactPerson,
            supplierFromDb.Phone,
            supplierFromDb.Email,
            supplierFromDb.Address,
            supplierFromDb.Notes,
            supplierFromDb.IsActive,
            supplierFromDb.CreatedAt,
            supplierFromDb.Code
        );

        return Ok(supplier);
    }

    // POST: api/suppliers
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] SupplierCreateDto request)
    {
        if (!string.IsNullOrEmpty(request.Code))
        {
            if (await _context.Suppliers.AnyAsync(s => s.Code == request.Code))
                return Conflict(new { message = "ეს კოდი უკვე გამოყენებულია" });
        }

        var newSupplier = new Supplier
        {
            Name = request.Name.Trim(),
            ContactPerson = request.ContactPerson?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            Notes = request.Notes?.Trim(),
            Code = request.Code?.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system"
        };

        _context.Suppliers.Add(newSupplier);
        await _context.SaveChangesAsync();

        var dto = new SupplierDto(
            newSupplier.Id,
            newSupplier.Name,
            newSupplier.ContactPerson,
            newSupplier.Phone,
            newSupplier.Email,
            newSupplier.Address,
            newSupplier.Notes,
            newSupplier.IsActive,
            newSupplier.CreatedAt,
            newSupplier.Code
        );

        return CreatedAtAction(nameof(GetSupplier), new { id = newSupplier.Id }, dto);
    }

    // PUT: api/suppliers/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] SupplierUpdateDto request)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            return NotFound(new { message = "მომწოდებელი არ მოიძებნა" });

        if (!string.IsNullOrEmpty(request.Code) && request.Code != supplier.Code)
        {
            if (await _context.Suppliers.AnyAsync(s => s.Code == request.Code))
                return Conflict(new { message = "ეს კოდი უკვე გამოყენებულია" });
        }

        if (!string.IsNullOrEmpty(request.Name))
            supplier.Name = request.Name.Trim();

        if (request.ContactPerson != null)
            supplier.ContactPerson = request.ContactPerson.Trim();

        if (request.Phone != null)
            supplier.Phone = request.Phone.Trim();

        if (request.Email != null)
            supplier.Email = request.Email.Trim();

        if (request.Address != null)
            supplier.Address = request.Address.Trim();

        if (request.Notes != null)
            supplier.Notes = request.Notes.Trim();

        if (!string.IsNullOrEmpty(request.Code))
            supplier.Code = request.Code.Trim();

        if (request.IsActive.HasValue)
            supplier.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/suppliers/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            return NotFound(new { message = "მომწოდებელი არ მოიძებნა" });

        var hasAssets = await _context.Assets.AnyAsync(a => a.SupplierId == id);
        if (hasAssets)
            return BadRequest(new { message = "მომწოდებელს აქვს დაკავშირებული აქტივები. წაშლა შეუძლებელია." });

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}