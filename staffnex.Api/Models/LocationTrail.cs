namespace staffnex.Api.Models;

public class LocationTrail
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Address { get; set; }
    public DateTime TrailDate { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public Staff? Staff { get; set; }
}