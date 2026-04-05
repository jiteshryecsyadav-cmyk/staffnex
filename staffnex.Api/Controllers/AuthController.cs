using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staffnex.Api.Data;
using staffnex.Api.DTOs;
using staffnex.Api.Services;

namespace staffnex.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext context, JwtService jwtService) : ApiControllerBase
{
    /// <summary>
    /// Authenticates an admin or staff user and returns a JWT token.
    /// </summary>
    /// <param name="request">Login payload containing username, password, and expected role.</param>
    /// <returns>JWT token and basic profile information.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiSuccessResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiSuccessResponse<LoginResponse>>> Login(LoginRequest request)
    {
        var user = await context.Users
            .Include(item => item.Staff)
            .FirstOrDefaultAsync(item => item.Username == request.Username);

        if (user is null || !user.IsActive)
        {
            return ApiUnauthorized("Invalid username or inactive user.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return ApiUnauthorized("Invalid username or password.");
        }

        if (!string.Equals(user.Role, request.Role, StringComparison.OrdinalIgnoreCase))
        {
            return ApiUnauthorized("Role mismatch.");
        }

        var token = jwtService.GenerateToken(user);
        var refreshToken = jwtService.CreateRefreshToken(user.Id);
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var response = new LoginResponse
        {
            Token = token,
            Role = user.Role,
            UserId = user.Id,
            StaffId = user.StaffId,
            FullName = user.Staff?.FullName ?? user.Username,
            EmployeeId = user.Staff?.EmployeeId ?? string.Empty,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt
        };

        return ApiOk(response, "Login successful.");
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new access token and rotated refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiSuccessResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiSuccessResponse<LoginResponse>>> Refresh(RefreshTokenRequest request)
    {
        var storedToken = await context.RefreshTokens
            .Include(item => item.User)
            .ThenInclude(item => item!.Staff)
            .FirstOrDefaultAsync(item => item.Token == request.RefreshToken);

        if (storedToken is null || !storedToken.IsActive || storedToken.User is null || !storedToken.User.IsActive)
        {
            return ApiUnauthorized("Invalid or expired refresh token.");
        }

        storedToken.RevokedAt = DateTime.UtcNow;

        var newAccessToken = jwtService.GenerateToken(storedToken.User);
        var newRefreshToken = jwtService.CreateRefreshToken(storedToken.UserId);
        context.RefreshTokens.Add(newRefreshToken);
        await context.SaveChangesAsync();

        var response = new LoginResponse
        {
            Token = newAccessToken,
            Role = storedToken.User.Role,
            UserId = storedToken.User.Id,
            StaffId = storedToken.User.StaffId,
            FullName = storedToken.User.Staff?.FullName ?? storedToken.User.Username,
            EmployeeId = storedToken.User.Staff?.EmployeeId ?? string.Empty,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiresAt = newRefreshToken.ExpiresAt
        };

        return ApiOk(response, "Token refreshed successfully.");
    }

    /// <summary>
    /// Revokes a refresh token to log out the current session.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiSuccessResponse<object>>> Logout(RefreshTokenRequest request)
    {
        var storedToken = await context.RefreshTokens.FirstOrDefaultAsync(item => item.Token == request.RefreshToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return ApiUnauthorized("Invalid or expired refresh token.");
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return ApiOk<object?>(null, "Logout successful.");
    }
}