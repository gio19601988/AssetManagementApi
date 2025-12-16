using AssetManagementApi.Models;
using AssetManagementApi.Data;
using AssetManagementApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;     // <--- ახალი: SymmetricSecurityKey, SigningCredentials, SecurityAlgorithms
using System.IdentityModel.Tokens.Jwt; // <--- ახალი: JwtSecurityToken, JwtSecurityTokenHandler
using System.Text;

namespace AssetManagementApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<AppUser>();
    }

    // POST: api/Auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "მომხმარებლის სახელი და პაროლი აუცილებელია" });

        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null)
            return Unauthorized(new { message = "არასწორი მომხმარებელი ან პაროლი" });

        if (string.IsNullOrEmpty(user.PasswordHash))
            return Unauthorized(new { message = "პაროლი არ არის დაყენებული. დაუკავშირდით ადმინისტრატორს." });

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "არასწორი მომხმარებელი ან პაროლი" });

        var token = GenerateJwtToken(user);

        return Ok(new LoginResponse(
            Token: token,
            Username: user.Username,
            FullName: user.FullName ?? user.Username,
            Role: user.Role
        ));
    }

    // POST: api/Auth/register (მხოლოდ Admin-ისთვის)
    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "მომხმარებლის სახელი და პაროლი აუცილებელია" });

        // უნიკალურობის შემოწმება
        var existingUser = await _context.AppUsers
            .AnyAsync(u => u.Username == request.Username);

        if (existingUser)
            return Conflict(new { message = "ეს მომხმარებლის სახელი უკვე არსებობს" });

        // ახალი მომხმარებლის შექმნა
        var newUser = new AppUser
        {
            Username = request.Username.Trim(),
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
            Role = request.Role.Trim(),
            IsActive = request.IsActive,
            // თუ გაქვს CreatedAt/CreatedBy ველები:
            // CreatedAt = DateTime.UtcNow,
            // CreatedBy = User.Identity?.Name ?? "system"
        };

        // პაროლის ჰეშირება
        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, request.Password);

        _context.AppUsers.Add(newUser);
        await _context.SaveChangesAsync();

        var response = new RegisterResponse(
            Id: newUser.Id,
            Username: newUser.Username,
            FullName: newUser.FullName ?? newUser.Username,
            Role: newUser.Role
        );

        return CreatedAtAction(nameof(Register), response);
    }

    // POST: api/Auth/set-initial-password (დროებითი – production-ში წაშალე!)
    [HttpPost("set-initial-password")]
    [AllowAnonymous]
    public async Task<IActionResult> SetInitialPassword([FromBody] SetPasswordRequest request)
    {
        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null)
            return NotFound(new { message = "მომხმარებელი არ მოიძებნა" });

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"პაროლი წარმატებით დაყენდა მომხმარებელ {request.Username}-სთვის" });
    }

    // POST: api/Auth/change-password
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return NotFound(new { message = "მომხმარებელი არ მოიძებნა ან პაროლი არ არის დაყენებული" });

        var verifyOld = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.OldPassword);
        if (verifyOld == PasswordVerificationResult.Failed)
            return BadRequest(new { message = "ძველი პაროლი არასწორია" });

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = "პაროლი წარმატებით შეიცვალა" });
    }

    private string GenerateJwtToken(AppUser user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role),
            new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}