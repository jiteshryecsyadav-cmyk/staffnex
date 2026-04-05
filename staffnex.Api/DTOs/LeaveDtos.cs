using System.ComponentModel.DataAnnotations;

namespace staffnex.Api.DTOs;

/// <summary>
/// Request payload to create a leave request.
/// </summary>
public class CreateLeaveRequestDto
{
    /// <example>2</example>
    [Range(1, int.MaxValue)]
    public int StaffId { get; set; }

    /// <example>2026-04-10T00:00:00Z</example>
    [Required]
    public DateTime LeaveDate { get; set; }

    /// <example>Medical appointment</example>
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request payload to update a pending leave request.
/// </summary>
public class UpdateLeaveRequestDto
{
    /// <example>2026-04-10T00:00:00Z</example>
    [Required]
    public DateTime LeaveDate { get; set; }

    /// <example>Medical appointment rescheduled</example>
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request payload for approving or rejecting a leave request.
/// </summary>
public class LeaveActionRequestDto
{
    /// <example>Approved for medical leave</example>
    [StringLength(500, MinimumLength = 3)]
    public string? Remarks { get; set; }
}

/// <summary>
/// Leave request response model.
/// </summary>
public class LeaveRequestDto
{
    /// <example>1</example>
    public int Id { get; set; }

    /// <example>2</example>
    public int StaffId { get; set; }

    /// <example>EMP002</example>
    public string EmployeeId { get; set; } = string.Empty;

    /// <example>Pooja Sharma</example>
    public string FullName { get; set; } = string.Empty;

    /// <example>2026-04-10T00:00:00Z</example>
    public DateTime LeaveDate { get; set; }

    /// <example>Medical appointment</example>
    public string Reason { get; set; } = string.Empty;

    /// <example>Pending</example>
    public string Status { get; set; } = string.Empty;

    /// <example>1</example>
    public int? ApprovedBy { get; set; }

    /// <example>admin</example>
    public string ApprovedByUsername { get; set; } = string.Empty;

    /// <example>Approved for medical leave</example>
    public string ActionRemarks { get; set; } = string.Empty;

    /// <example>2026-04-05T12:30:00Z</example>
    public DateTime CreatedAt { get; set; }

    /// <example>2026-04-05T12:30:00Z</example>
    public DateTime UpdatedAt { get; set; }
}