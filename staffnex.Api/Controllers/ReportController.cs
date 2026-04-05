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
[Route("api/report")]
[Authorize]
public class ReportController(AppDbContext context) : ApiControllerBase
{
    /// <summary>
    /// Returns summary counts and recent check-ins for today's dashboard.
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResponse<DashboardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponse<DashboardDto>>> GetDashboard()
    {
        var today = DateTime.UtcNow.Date;
        var activeStaffIds = await context.Staffs
            .Where(staff => staff.IsActive)
            .Select(staff => staff.Id)
            .ToListAsync();

        var todayLogs = await context.AttendanceLogs
            .Where(log => log.LogDate == today && activeStaffIds.Contains(log.StaffId))
            .ToListAsync();

        var leaveStaffIds = await context.LeaveRequests
            .Where(request => request.LeaveDate == today && request.Status == "Approved" && activeStaffIds.Contains(request.StaffId))
            .Select(request => request.StaffId)
            .Distinct()
            .ToListAsync();

        var presentIds = todayLogs.Where(log => log.Status == "Present").Select(log => log.StaffId).Distinct().ToHashSet();
        var halfDayIds = todayLogs.Where(log => log.Status == "Half-Day").Select(log => log.StaffId).Distinct().ToHashSet();
        var absentExcludedIds = presentIds.Union(halfDayIds).Union(leaveStaffIds).ToHashSet();

        var recentCheckIns = todayLogs
            .Where(log => log.CheckInTime.HasValue)
            .OrderByDescending(log => log.CheckInTime)
            .Take(10)
            .Select(MapAttendanceLog)
            .ToList();

        var dashboard = new DashboardDto
        {
            TotalStaff = activeStaffIds.Count,
            PresentToday = presentIds.Count,
            HalfDayToday = halfDayIds.Count,
            OnLeaveToday = leaveStaffIds.Count,
            AbsentToday = activeStaffIds.Count(id => !absentExcludedIds.Contains(id)),
            RecentCheckIns = recentCheckIns
        };

        return ApiOk(dashboard, "Dashboard fetched successfully.");
    }

    /// <summary>
    /// Returns monthly attendance and salary performance for all active staff.
    /// </summary>
    [HttpGet("performance")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResponse<IEnumerable<PerformanceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiSuccessResponse<IEnumerable<PerformanceDto>>>> GetPerformance([FromQuery] int year, [FromQuery] int month)
    {
        if (!IsValidMonth(year, month))
        {
            return ApiBadRequest("Invalid year or month.");
        }

        var performance = await BuildPerformanceAsync(year, month, null);
        return ApiOk<IEnumerable<PerformanceDto>>(performance, "Performance report fetched successfully.");
    }

    /// <summary>
    /// Returns monthly attendance and salary performance for a single staff member.
    /// </summary>
    [HttpGet("performance/{staffId:int}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<PerformanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponse<PerformanceDto>>> GetPerformanceByStaff(int staffId, [FromQuery] int year, [FromQuery] int month)
    {
        if (!CanAccessStaff(staffId))
        {
            return ApiForbidden("You are not allowed to view performance for another staff member.");
        }

        if (!IsValidMonth(year, month))
        {
            return ApiBadRequest("Invalid year or month.");
        }

        var performance = await BuildPerformanceAsync(year, month, staffId);
        var item = performance.FirstOrDefault();

        if (item is null)
        {
            return ApiNotFound("Performance record not found.");
        }

        return ApiOk(item, "Staff performance fetched successfully.");
    }

    private async Task<List<PerformanceDto>> BuildPerformanceAsync(int year, int month, int? staffId)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);
        var totalWorkingDays = Enumerable.Range(0, (endDate - startDate).Days)
            .Select(offset => startDate.AddDays(offset))
            .Count(date => date.DayOfWeek != DayOfWeek.Sunday);

        var staffQuery = context.Staffs
            .Where(staff => staff.IsActive);

        if (staffId.HasValue)
        {
            staffQuery = staffQuery.Where(staff => staff.Id == staffId.Value);
        }

        var staffList = await staffQuery
            .OrderBy(staff => staff.FullName)
            .ToListAsync();

        var staffIds = staffList.Select(staff => staff.Id).ToList();

        var attendanceLogs = await context.AttendanceLogs
            .Where(log => staffIds.Contains(log.StaffId) && log.LogDate >= startDate && log.LogDate < endDate)
            .ToListAsync();

        var leaveRequests = await context.LeaveRequests
            .Where(request => staffIds.Contains(request.StaffId) && request.LeaveDate >= startDate && request.LeaveDate < endDate && request.Status == "Approved")
            .ToListAsync();

        return staffList.Select(staff =>
        {
            var logs = attendanceLogs.Where(log => log.StaffId == staff.Id).ToList();
            var presentDays = logs.Count(log => log.Status == "Present");
            var halfDays = logs.Count(log => log.Status == "Half-Day");
            var leaveDays = leaveRequests.Count(request => request.StaffId == staff.Id && request.LeaveDate.DayOfWeek != DayOfWeek.Sunday);
            var absentDays = Math.Max(totalWorkingDays - presentDays - halfDays - leaveDays, 0);
            var avgWorkingHours = Math.Round(logs.Where(log => log.CheckOutTime.HasValue).Select(log => log.WorkingHours).DefaultIfEmpty(0m).Average(), 2);
            var attendancePercent = totalWorkingDays == 0
                ? 0m
                : Math.Round((((decimal)presentDays + (halfDays * 0.5m) + leaveDays) / totalWorkingDays) * 100m, 2);
            var perDaySalary = totalWorkingDays == 0 ? 0m : Math.Round(staff.MonthlySalary / totalWorkingDays, 2);
            var deduction = Math.Round(absentDays * perDaySalary, 2);
            var netSalary = Math.Round(staff.MonthlySalary - deduction, 2);

            return new PerformanceDto
            {
                StaffId = staff.Id,
                EmployeeId = staff.EmployeeId,
                FullName = staff.FullName,
                Designation = staff.Designation,
                MonthlySalary = staff.MonthlySalary,
                TotalWorkingDays = totalWorkingDays,
                PresentDays = presentDays,
                AbsentDays = absentDays,
                HalfDays = halfDays,
                LeaveDays = leaveDays,
                AvgWorkingHours = avgWorkingHours,
                AttendancePercent = attendancePercent,
                Deduction = deduction,
                NetSalary = netSalary,
                Rating = GetRating(attendancePercent)
            };
        }).ToList();
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

    private static string GetRating(decimal attendancePercent)
    {
        if (attendancePercent >= 95m)
        {
            return "Excellent";
        }

        if (attendancePercent >= 80m)
        {
            return "Good";
        }

        if (attendancePercent >= 60m)
        {
            return "Average";
        }

        return "Poor";
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