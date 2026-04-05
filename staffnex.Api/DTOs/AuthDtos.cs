using System.ComponentModel.DataAnnotations;

namespace staffnex.Api.DTOs;

/// <summary>
/// Login request for admin or staff users.
/// </summary>
public class LoginRequest
{
    /// <example>admin</example>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    /// <example>Admin@123</example>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    /// <example>Admin</example>
    [Required]
    [RegularExpression("^(Admin|Staff)$", ErrorMessage = "Role must be Admin or Staff.")]
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Auth response returned after successful login.
/// </summary>
public class LoginResponse
{
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string Token { get; set; } = string.Empty;

    /// <example>Admin</example>
    public string Role { get; set; } = string.Empty;

    /// <example>1</example>
    public int UserId { get; set; }

    /// <example>1</example>
    public int? StaffId { get; set; }

    /// <example>Raj Verma</example>
    public string FullName { get; set; } = string.Empty;

    /// <example>EMP001</example>
    public string EmployeeId { get; set; } = string.Empty;

    /// <example>3w1f2x...refresh-token...</example>
    public string RefreshToken { get; set; } = string.Empty;

    /// <example>2026-04-12T12:00:00Z</example>
    public DateTime RefreshTokenExpiresAt { get; set; }
}

/// <summary>
/// Refresh token request payload.
/// </summary>
public class RefreshTokenRequest
{
    /// <example>3w1f2x...refresh-token...</example>
    [Required]
    [StringLength(500)]
    public string RefreshToken { get; set; } = string.Empty;
}