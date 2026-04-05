namespace staffnex.Api.Models;

public class LeaveRequest
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public DateTime LeaveDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int? ApprovedBy { get; set; }
    public string? ActionRemarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Staff? Staff { get; set; }
}