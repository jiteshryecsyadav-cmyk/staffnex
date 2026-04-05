using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staffnex.Api.Data;
using staffnex.Api.DTOs;
using staffnex.Api.Models;

namespace staffnex.Api.Controllers;

[ApiController]
[Route("api/location")]
[Authorize]
public class LocationController(AppDbContext context) : ApiControllerBase
{
    /// <summary>
    /// Saves a location trail point for a staff member.
    /// </summary>
    [HttpPost("update")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponse<object>>> Update(LocationUpdateRequest request)
    {
        if (!CanAccessStaff(request.StaffId))
        {
            return ApiForbidden("You are not allowed to update location for another staff member.");
        }

        var staff = await context.Staffs.FirstOrDefaultAsync(item => item.Id == request.StaffId && item.IsActive);
        if (staff is null)
        {
            return ApiNotFound("Active staff not found.");
        }

        var now = DateTime.UtcNow;
        var trail = new LocationTrail
        {
            StaffId = request.StaffId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address,
            TrailDate = now.Date,
            RecordedAt = now
        };

        context.LocationTrails.Add(trail);
        await context.SaveChangesAsync();

        return ApiOk(new
        {
            trail.Id,
            trail.StaffId,
            trail.Latitude,
            trail.Longitude,
            trail.Address,
            trail.TrailDate,
            trail.RecordedAt
        }, "Location updated successfully.");
    }

    /// <summary>
    /// Returns the latest active location for each staff member for today.
    /// </summary>
    [HttpGet("all-active")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponse<IEnumerable<object>>>> GetAllActive()
    {
        var today = DateTime.UtcNow.Date;
        var latestTrails = await context.LocationTrails
            .Include(item => item.Staff)
            .Where(item => item.TrailDate == today && item.Staff != null && item.Staff.IsActive)
            .OrderByDescending(item => item.RecordedAt)
            .ToListAsync();

        var result = latestTrails
            .GroupBy(item => item.StaffId)
            .Select(group => group.First())
            .OrderBy(item => item.Staff!.FullName)
            .Select(item => new
            {
                item.StaffId,
                EmployeeId = item.Staff!.EmployeeId,
                FullName = item.Staff.FullName,
                item.Latitude,
                item.Longitude,
                item.Address,
                item.TrailDate,
                item.RecordedAt
            });

        return ApiOk(result.ToList(), "Active locations fetched successfully.");
    }

    /// <summary>
    /// Returns the location trail for a staff member on a given date.
    /// </summary>
    [HttpGet("trail/{staffId:int}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiSuccessResponse<IEnumerable<object>>>> GetTrail(int staffId, [FromQuery] DateTime? date)
    {
        if (!CanAccessStaff(staffId))
        {
            return ApiForbidden("You are not allowed to view location trail for another staff member.");
        }

        var selectedDate = (date ?? DateTime.UtcNow.Date).Date;
        var trails = await context.LocationTrails
            .Where(item => item.StaffId == staffId && item.TrailDate == selectedDate)
            .OrderBy(item => item.RecordedAt)
            .Select(item => new
            {
                item.Id,
                item.StaffId,
                item.Latitude,
                item.Longitude,
                item.Address,
                item.TrailDate,
                item.RecordedAt
            })
            .ToListAsync();

        return ApiOk(trails, "Location trail fetched successfully.");
    }

    private bool CanAccessStaff(int staffId)
    {
        return IsAdmin() || GetCurrentStaffId() == staffId;
    }

    private bool IsAdmin()
    {
        return string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private int? GetCurrentStaffId()
    {
        return int.TryParse(User.FindFirstValue("staffId"), out var staffId) ? staffId : null;
    }
}