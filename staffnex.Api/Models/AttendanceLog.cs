namespace staffnex.Api.Models;

public class AttendanceLog
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
    public string Status { get; set; } = "Absent";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Staff? Staff { get; set; }
}