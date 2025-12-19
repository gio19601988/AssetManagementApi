using AssetManagementApi.Data;
using AssetManagementApi.DTOs;
using AssetManagementApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmployeesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/employees
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees()
    {
        var employeesFromDb = await _context.Employees
            .Include(e => e.Department)
            .OrderBy(e => e.FullName)
            .ToListAsync();

        var employees = employeesFromDb.Select(e => new EmployeeDto(
            e.Id,
            e.FullName,           // Name
            e.FullName,
            e.Position,
            e.DepartmentId,
            e.Department?.Name,
            e.Phone,
            e.Email
        )).ToList();

        return Ok(employees);
    }

    // GET: api/employees/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<EmployeeDto>> GetEmployee(int id)
    {
        var employeeFromDb = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employeeFromDb == null)
            return NotFound(new { message = "თანამშრომელი არ მოიძებნა" });

        var employee = new EmployeeDto(
            employeeFromDb.Id,
            employeeFromDb.FullName,
            employeeFromDb.FullName,
            employeeFromDb.Position,
            employeeFromDb.DepartmentId,
            employeeFromDb.Department?.Name,
            employeeFromDb.Phone,
            employeeFromDb.Email
        );

        return Ok(employee);
    }

    // POST: api/employees
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EmployeeDto>> CreateEmployee([FromBody] EmployeeCreateDto request)
    {
        if (request.DepartmentId.HasValue && !await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId))
            return BadRequest(new { message = "დეპარტამენტი არ მოიძებნა" });

        var newEmployee = new Employee
        {
            FullName = request.FullName.Trim(),
            Position = request.Position?.Trim(),
            DepartmentId = request.DepartmentId,
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim()
        };

        _context.Employees.Add(newEmployee);
        await _context.SaveChangesAsync();

        await _context.Entry(newEmployee).Reference(e => e.Department).LoadAsync();

        var dto = new EmployeeDto(
            newEmployee.Id,
            newEmployee.FullName,     // Name
            newEmployee.FullName,
            newEmployee.Position,
            newEmployee.DepartmentId,
            newEmployee.Department?.Name,
            newEmployee.Phone,
            newEmployee.Email
        );

        return CreatedAtAction(nameof(GetEmployee), new { id = newEmployee.Id }, dto);
    }

    // PUT და DELETE — უცვლელი (კარგია)
    // ... (შენი კოდი)
}