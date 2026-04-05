using System.ComponentModel.DataAnnotations;

namespace staffnex.Api.DTOs;

/// <summary>
/// Request payload for check-in.
/// </summary>
public class CheckInRequest
{
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int StaffId { get; set; }

    /// <example>26.912434</example>
    [Range(typeof(decimal), "-90", "90")]
    public decimal? Latitude { get; set; }

    /// <example>75.787270</example>
    [Range(typeof(decimal), "-180", "180")]
    public decimal? Longitude { get; set; }

    /// <example>Jaipur Office</example>
    [StringLength(300)]
    public string? Address { get; set; }
}

/// <summary>
/// Request payload for check-out.
/// </summary>
public class CheckOutRequest
{
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int StaffId { get; set; }

    /// <example>26.912500</example>
    [Range(typeof(decimal), "-90", "90")]
    public decimal? Latitude { get; set; }

    /// <example>75.787300</example>
    [Range(typeof(decimal), "-180", "180")]
    public decimal? Longitude { get; set; }

    /// <example>Jaipur Office Exit</example>
    [StringLength(300)]
    public string? Address { get; set; }
}

/// <summary>
/// Request payload for live location updates.
/// </summary>
public class LocationUpdateRequest
{
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int StaffId { get; set; }

    /// <example>26.912600</example>
    [Range(typeof(decimal), "-90", "90")]
    public decimal Latitude { get; set; }

    /// <example>75.787350</example>
    [Range(typeof(decimal), "-180", "180")]
    public decimal Longitude { get; set; }

    /// <example>Client Visit Route</example>
    [StringLength(300)]
    public string? Address { get; set; }
}

public class AttendanceLogDto
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public DateTime LogDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public decimal? CheckInLat { get; set; }
    public decimal? CheckInLng { get; set; }
    public string? CheckInAddress { get; set; }
    public decimal? CheckOutLat { get; set; }
    public decimal? CheckOutLng { get; set; }
    public string? CheckOutAddress { get; set; }
    public decimal WorkingHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DashboardDto
{
    public int TotalStaff { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public int OnLeaveToday { get; set; }
    public int HalfDayToday { get; set; }
    public List<AttendanceLogDto> RecentCheckIns { get; set; } = new();
}

public class PerformanceDto
{
    public int StaffId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public decimal MonthlySalary { get; set; }
    public int TotalWorkingDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int HalfDays { get; set; }
    public int LeaveDays { get; set; }
    public decimal AvgWorkingHours { get; set; }
    public decimal AttendancePercent { get; set; }
    public decimal Deduction { get; set; }
    public decimal NetSalary { get; set; }
    public string Rating { get; set; } = string.Empty;
}