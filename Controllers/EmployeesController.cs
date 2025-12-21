using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using AssetManagementApi.Models;

namespace AssetManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmployeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Employees
        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            return await _context.Employees
                .Include(e => e.Department)
                .OrderBy(e => e.FullName)
                .ToListAsync();
        }

        // GET: api/Employees/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null) return NotFound();
            return employee;
        }

        // POST: api/Employees
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Employee>> CreateEmployee(Employee employee)
        {
            employee.CreatedAt = DateTime.UtcNow;  // ← აუცილებლად!
            employee.CreatedBy = User.Identity?.Name ?? "Unknown";

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }

        // PUT: api/Employees/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEmployee(int id, Employee employee)
        {
            if (id != employee.Id) return BadRequest("ID mismatch");

            var existing = await _context.Employees.FindAsync(id);
            if (existing == null) return NotFound();

            existing.FullName = employee.FullName;
            existing.Position = employee.Position;
            existing.DepartmentId = employee.DepartmentId;
            existing.Phone = employee.Phone;
            existing.Email = employee.Email;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = User.Identity?.Name ?? "Unknown";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("Concurrency error");
            }

            return NoContent();
        }

        // DELETE: api/Employees/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}