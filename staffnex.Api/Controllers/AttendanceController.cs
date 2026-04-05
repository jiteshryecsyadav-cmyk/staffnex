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
[Route("api/attendance")]
[Authorize]
public class AttendanceController(AppDbContext context) : ApiControllerBase
{
    /// <summary>
    /// Creates or updates the current day's check-in record for a staff member.
    /// </summary>
    [HttpPost("checkin")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AttendanceLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponse<AttendanceLogDto>>> CheckIn(CheckInRequest request)
    {
        if (!CanAccessStaff(request.StaffId))
        {
            return ApiForbidden("You are not allowed to mark attendance for another staff member.");
        }

        var staff = await context.Staffs.FirstOrDefaultAsync(item => item.Id == request.StaffId && item.IsActive);
        if (staff is null)
        {
            return ApiNotFound("Active staff not found.");
        }

        var now = DateTime.UtcNow;
        var today = now.Date;

        var log = await context.AttendanceLogs.FirstOrDefaultAsync(item => item.StaffId == request.StaffId && item.LogDate == today);
        if (log is null)
        {
            log = new AttendanceLog
            {
                StaffId = request.StaffId,
                LogDate = today,
                CreatedAt = now
            };
            context.AttendanceLogs.Add(log);
        }

        log.CheckInTime = now;
        log.CheckInLat = request.Latitude;
        log.CheckInLng = request.Longitude;
        log.CheckInAddress = request.Address;
        log.Status = "Present";
        log.UpdatedAt = now;

        await context.SaveChangesAsync();
        return ApiOk(MapAttendanceLog(log), "Check-in recorded successfully.");
    }

    /// <summary>
    /// Completes the current day's attendance record and calculates working hours.
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AttendanceLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiSuccessResponse<AttendanceLogDto>>> CheckOut(CheckOutRequest request)
    {
        if (!CanAccessStaff(request.StaffId))
        {
            return ApiForbidden("You are not allowed to mark attendance for another staff member.");
        }

        var now = DateTime.UtcNow;
        var today = now.Date;

        var log = await context.AttendanceLogs.FirstOrDefaultAsync(item => item.StaffId == request.StaffId && item.LogDate == today);
        if (log is null || log.CheckInTime is null)
        {
            return ApiBadRequest("Check-in is required before checkout.");
        }

        log.CheckOutTime = now;
        log.CheckOutLat = request.Latitude;
        log.CheckOutLng = request.Longitude;
        log.CheckOutAddress = request.Address;
        log.WorkingHours = Math.Round((decimal)(now - log.CheckInTime.Value).TotalHours, 2);
        log.Status = log.WorkingHours < 4m ? "Half-Day" : "Present";
        log.UpdatedAt = now;

        await context.SaveChangesAsync();
        return ApiOk(MapAttendanceLog(log), "Check-out recorded successfully.");
    }

    /// <summary>
    /// Returns today's attendance record for a staff member.
    /// </summary>
    [HttpGet("today/{staffId:int}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AttendanceLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponse<AttendanceLogDto>>> GetToday(int staffId)
    {
        if (!CanAccessStaff(staffId))
        {
            return ApiForbidden("You are not allowed to view attendance for another staff member.");
        }

        var today = DateTime.UtcNow.Date;
        var log = await context.AttendanceLogs.FirstOrDefaultAsync(item => item.StaffId == staffId && item.LogDate == today);

        if (log is null)
        {
            return ApiNotFound("Attendance log not found for today.");
        }

        return ApiOk(MapAttendanceLog(log), "Today's attendance fetched successfully.");
    }

    /// <summary>
    /// Returns today's attendance records for all staff members.
    /// </summary>
    [HttpGet("today-all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResponse<PagedResult<AttendanceLogDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponse<PagedResult<AttendanceLogDto>>>> GetTodayAll([FromQuery] AttendanceListQuery query)
    {
        var today = DateTime.UtcNow.Date;
        var logsQuery = context.AttendanceLogs
            .Where(item => item.LogDate == today)
            .AsQueryable();

        if (query.StaffId.HasValue)
        {
            logsQuery = logsQuery.Where(item => item.StaffId == query.StaffId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            logsQuery = logsQuery.Where(item => item.Status == query.Status);
        }

        logsQuery = ApplySorting(logsQuery, query);

        var totalCount = await logsQuery.CountAsync();
        var logs = await logsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return ApiOk(ToPagedResult(logs.Select(MapAttendanceLog).ToList(), query.Page, query.PageSize, totalCount), "Today's attendance list fetched successfully.");
    }

    /// <summary>
    /// Returns monthly attendance records for a staff member.
    /// </summary>
    [HttpGet("monthly/{staffId:int}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<PagedResult<AttendanceLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiSuccessResponse<PagedResult<AttendanceLogDto>>>> GetMonthly(int staffId, [FromQuery] int year, [FromQuery] int month, [FromQuery] AttendanceListQuery query)
    {
        if (!CanAccessStaff(staffId))
        {
            return ApiForbidden("You are not allowed to view attendance for another staff member.");
        }

        if (!IsValidMonth(year, month))
        {
            return ApiBadRequest("Invalid year or month.");
        }

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var logsQuery = context.AttendanceLogs
            .Where(item => item.StaffId == staffId && item.LogDate >= startDate && item.LogDate < endDate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            logsQuery = logsQuery.Where(item => item.Status == query.Status);
        }

        logsQuery = ApplySorting(logsQuery, query);

        var totalCount = await logsQuery.CountAsync();
        var logs = await logsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return ApiOk(ToPagedResult(logs.Select(MapAttendanceLog).ToList(), query.Page, query.PageSize, totalCount), "Monthly attendance fetched successfully.");
    }

    /// <summary>
    /// Returns monthly attendance records for all staff members.
    /// </summary>
    [HttpGet("all-monthly")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResponse<PagedResult<AttendanceLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiSuccessResponse<PagedResult<AttendanceLogDto>>>> GetAllMonthly([FromQuery] int year, [FromQuery] int month, [FromQuery] AttendanceListQuery query)
    {
        if (!IsValidMonth(year, month))
        {
            return ApiBadRequest("Invalid year or month.");
        }

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var logsQuery = context.AttendanceLogs
            .Where(item => item.LogDate >= startDate && item.LogDate < endDate)
            .AsQueryable();

        if (query.StaffId.HasValue)
        {
            logsQuery = logsQuery.Where(item => item.StaffId == query.StaffId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            logsQuery = logsQuery.Where(item => item.Status == query.Status);
        }

        logsQuery = ApplySorting(logsQuery, query);

        var totalCount = await logsQuery.CountAsync();
        var logs = await logsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return ApiOk(ToPagedResult(logs.Select(MapAttendanceLog).ToList(), query.Page, query.PageSize, totalCount), "Monthly attendance list fetched successfully.");
    }

    private static IQueryable<AttendanceLog> ApplySorting(IQueryable<AttendanceLog> query, AttendanceListQuery request)
    {
        var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return request.SortBy?.ToLowerInvariant() switch
        {
            "logdate" => descending ? query.OrderByDescending(item => item.LogDate).ThenByDescending(item => item.StaffId) : query.OrderBy(item => item.LogDate).ThenBy(item => item.StaffId),
            "staffid" => descending ? query.OrderByDescending(item => item.StaffId).ThenByDescending(item => item.LogDate) : query.OrderBy(item => item.StaffId).ThenBy(item => item.LogDate),
            "workinghours" => descending ? query.OrderByDescending(item => item.WorkingHours) : query.OrderBy(item => item.WorkingHours),
            "status" => descending ? query.OrderByDescending(item => item.Status) : query.OrderBy(item => item.Status),
            _ => descending ? query.OrderByDescending(item => item.LogDate).ThenByDescending(item => item.StaffId) : query.OrderBy(item => item.LogDate).ThenBy(item => item.StaffId)
        };
    }

    private static AttendanceLogDto MapAttendanceLog(AttendanceLog log)
    {
        return new AttendanceLogDto
        {
            Id = log.Id,
            StaffId = log.StaffId,
            LogDate = log.LogDate,
            CheckInTime = log.CheckInTime,
            CheckOutTime = log.CheckOutTime,
            CheckInLat = log.CheckInLat,
            CheckInLng = log.CheckInLng,
            CheckInAddress = log.CheckInAddress,
            CheckOutLat = log.CheckOutLat,
            CheckOutLng = log.CheckOutLng,
            CheckOutAddress = log.CheckOutAddress,
            WorkingHours = log.WorkingHours,
            Status = log.Status,
            Notes = log.Notes,
            CreatedAt = log.CreatedAt,
            UpdatedAt = log.UpdatedAt
        };
    }

    private static bool IsValidMonth(int year, int month)
    {
        return year >= 2000 && year <= 3000 && month >= 1 && month <= 12;
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