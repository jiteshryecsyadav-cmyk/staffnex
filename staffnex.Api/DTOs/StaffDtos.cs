using System.ComponentModel.DataAnnotations;

namespace staffnex.Api.DTOs;

/// <summary>
/// Request to create a new staff profile and linked user account.
/// </summary>
public class CreateStaffRequest
{
    /// <example>Raj Verma</example>
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    /// <example>9876543210</example>
    [Required]
    [RegularExpression("^[0-9]{10,15}$", ErrorMessage = "Phone must contain 10 to 15 digits.")]
    public string Phone { get; set; } = string.Empty;

    /// <example>raj.verma@staffnex.local</example>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <example>Field Executive</example>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Designation { get; set; } = string.Empty;

    /// <example>2</example>
    public int? DepartmentId { get; set; }

    /// <example>32000</example>
    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal MonthlySalary { get; set; }

    /// <example>2025-01-10T00:00:00Z</example>
    [Required]
    public DateTime JoinDate { get; set; }

    /// <example>raj.staff</example>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    /// <example>Staff@123</example>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request to update an existing staff profile.
/// </summary>
public class UpdateStaffRequest
{
    /// <example>Raj Verma</example>
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    /// <example>9876543210</example>
    [Required]
    [RegularExpression("^[0-9]{10,15}$", ErrorMessage = "Phone must contain 10 to 15 digits.")]
    public string Phone { get; set; } = string.Empty;

    /// <example>raj.verma@staffnex.local</example>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <example>Field Executive</example>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Designation { get; set; } = string.Empty;

    /// <example>2</example>
    public int? DepartmentId { get; set; }

    /// <example>32000</example>
    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal MonthlySalary { get; set; }

    /// <example>2025-01-10T00:00:00Z</example>
    [Required]
    public DateTime JoinDate { get; set; }

    /// <example>raj.staff</example>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    /// <example>true</example>
    public bool IsActive { get; set; }

    /// <example>Staff@456</example>
    [StringLength(100, MinimumLength = 6)]
    public string? NewPassword { get; set; }
}

public class StaffDto
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public decimal MonthlySalary { get; set; }
    public DateTime JoinDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Username { get; set; } = string.Empty;
}