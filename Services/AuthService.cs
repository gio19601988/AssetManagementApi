using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using AssetManagementApi.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AssetManagementApi.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<AppUser>();
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return null;

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
            return null;

        // JWT ტოკენის გენერაცია
        var token = GenerateJwtToken(user);

        return new LoginResponse(
            Token: token,
            Username: user.Username,
            FullName: user.FullName ?? user.Username,
            Role: user.Role
        );
    }

    private string GenerateJwtToken(AppUser user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8), // 8 საათი
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // სურვილისამებრ: პაროლის შეცვლა
    public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
    {
        var user = await _context.AppUsers.FindAsync(userId);
        if (user == null) return false;

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await _context.SaveChangesAsync();
        return true;
    }
}