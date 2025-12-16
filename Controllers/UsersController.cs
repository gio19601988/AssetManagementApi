using AssetManagementApi.Models;
using AssetManagementApi.Data;
using AssetManagementApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AssetManagementApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Users — სრული სია
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var usersFromDb = await _context.AppUsers
            .OrderBy(u => u.Username)
            .ToListAsync();

        var users = usersFromDb.Select(u => new UserDto(
            u.Id,
            u.Username,
            u.FullName ?? u.Username,
            u.Role,
            u.IsActive,
            u.CreatedAt  // თუ არ გაქვს CreatedAt — წაშალე ეს პარამეტრი
        )).ToList();

        return Ok(users);
    }

    // GET: api/Users/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var userFromDb = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Id == id);

        if (userFromDb == null)
            return NotFound(new { message = "მომხმარებელი არ მოიძებნა" });

        var user = new UserDto(
            userFromDb.Id,
            userFromDb.Username,
            userFromDb.FullName ?? userFromDb.Username,
            userFromDb.Role,
            userFromDb.IsActive,
            userFromDb.CreatedAt
        );

        return Ok(user);
    }

    // PUT და DELETE უცვლელი რჩება (რადგან იქ FindAsync იყენებ)
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "მომხმარებელი არ მოიძებნა" });

        if (!string.IsNullOrWhiteSpace(request.Role))
            user.Role = request.Role.Trim();

        user.IsActive = request.IsActive;

        try
        {
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "განახლების შეცდომა: " + ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "მომხმარებელი არ მოიძებნა" });

        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || int.Parse(currentUserIdClaim) == id)
            return BadRequest(new { message = "თავად ადმინისტრატორის წაშლა შეუძლებელია" });

        _context.AppUsers.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}