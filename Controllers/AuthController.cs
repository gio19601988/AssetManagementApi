using AssetManagementApi.Models;
using AssetManagementApi.Data;
using AssetManagementApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;  // Encoding.UTF8-ისთვის

namespace AssetManagementApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<AppUser> _passwordHasher;  // Models. წაშალე!

    public AuthController(ApplicationDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<AppUser>();  // Models. წაშალე!
    }

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

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var username = User.Identity?.Name;
        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return NotFound();

        var verifyOld = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.OldPassword);
        if (verifyOld == PasswordVerificationResult.Failed)
            return BadRequest(new { message = "ძველი პაროლი არასწორია" });

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = "პაროლი წარმატებით შეიცვალა" });
    }

    private string GenerateJwtToken(AppUser user)  // Models. წაშალე!
    {
        var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var jwtSettings = configuration.GetSection("Jwt");

        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);  // System.Text. აღარ გჭირდება სრულად
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role),
            new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
            Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}

// DTO-ები გადაიტანე DTOs საქაღალდეში! აქ არ უნდა იყოს.