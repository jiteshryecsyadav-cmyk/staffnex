using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using staffnex.Api.Models;

namespace staffnex.Api.Services;

public class JwtService(IConfiguration configuration)
{
    public string GenerateToken(User user)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "staffnex";
        var audience = configuration["Jwt:Audience"] ?? "staffnex-clients";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("staffId", user.StaffId?.ToString() ?? string.Empty),
            new Claim("fullName", user.Staff?.FullName ?? string.Empty),
            new Claim("employeeId", user.Staff?.EmployeeId ?? string.Empty)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetAccessTokenLifetimeMinutes()),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken CreateRefreshToken(int userId)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays()),
            CreatedAt = DateTime.UtcNow
        };
    }

    private int GetAccessTokenLifetimeMinutes()
    {
        return int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var minutes) ? minutes : 60;
    }

    private int GetRefreshTokenLifetimeDays()
    {
        return int.TryParse(configuration["Jwt:RefreshTokenDays"], out var days) ? days : 7;
    }
}