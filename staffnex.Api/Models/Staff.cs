namespace staffnex.Api.Models;

public class Staff
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public decimal MonthlySalary { get; set; }
    public DateTime JoinDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Department? Department { get; set; }
    public User? User { get; set; }
    public ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
    public ICollection<LocationTrail> LocationTrails { get; set; } = new List<LocationTrail>();
}