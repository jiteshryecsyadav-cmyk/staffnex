using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staffnex.Api.Data;
using staffnex.Api.DTOs;
using staffnex.Api.Models;

namespace staffnex.Api.Controllers;

[ApiController]
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController(AppDbContext context) : ApiControllerBase
{
    /// <summary>
    /// Returns leave requests. Admin gets all records; staff users get only their own records.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponse<PagedResult<LeaveRequestDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponse<PagedResult<LeaveRequestDto>>>> GetAll([FromQuery] LeaveRequestListQuery query)
    {
        var leaveQuery = context.LeaveRequests
            .Include(item => item.Staff)
            .AsQueryable();

        if (!IsAdmin())
        {
            var currentStaffId = GetCurrentStaffId();
            if (!currentStaffId.HasValue)
            {
                return ApiForbidden("Staff identity was not found in token claims.");
            }

            leaveQuery = leaveQuery.Where(item => item.StaffId == currentStaffId.Value);
        }

        if (query.StaffId.HasValue && IsAdmin())
        {
            leaveQuery = leaveQuery.Where(item => item.StaffId == query.StaffId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            leaveQuery = leaveQuery.Where(item => item.Status == query.Status);
        }

        if (query.FromDate.HasValue)
        {
            leaveQuery = leaveQuery.Where(item => item.LeaveDate >= query.FromDate.Value.Date);
        }

        if (query.ToDate.HasValue)
        {
            leaveQuery = leaveQuery.Where(item => item.LeaveDate <= query.ToDate.Value.Date);
        }

        leaveQuery = ApplySorting(leaveQuery, query);
        var totalCount = await leaveQuery.CountAsync();

        var leaveRequests = await leaveQuery
            .OrderByDescending(item => item.LeaveDate)
            .ThenByDescending(item => item.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return ApiOk(ToPagedResult(await MapLeaveRequestsAsync(leaveRequests), query.Page, query.PageSize, totalCount), "Leave requests fetched successfully.");
    }

    /// <summary>
    /// Returns a single leave request by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponse<LeaveRequestDto>>> GetById(int id)
    {
        var leaveRequest = await context.LeaveRequests
            .Include(item => item.Staff)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (leaveRequest is null)
        {
            return ApiNotFound("Leave request not found.");
        }

        if (!CanAccessStaff(leaveRequest.StaffId))
        {
            return ApiForbidden("You are not allowed to access another staff member's leave request.");
        }

        return ApiOk(await MapLeaveRequestAsync(leaveRequest), "Leave request fetched successfully.");
    }

    /// <summary>
    /// Returns leave requests for a specific staff member.
    /// </summary>
    [HttpGet("staff/{staffId:int}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<PagedResult<LeaveRequestDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiSuccessResponse<PagedResult<LeaveRequestDto>>>> GetByStaff(int staffId, [FromQuery] LeaveRequestListQuery query)
    {
        if (!CanAccessStaff(staffId))
        {
            return ApiForbidden("You are not allowed to access another staff member's leave requests.");
        }

        var leaveQuery = context.LeaveRequests
            .Include(item => item.Staff)
            .Where(item => item.StaffId == staffId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            leaveQuery = leaveQuery.Where(item => item.Status == query.Status);
        }

        if (query.FromDate.HasValue)
        {
            leaveQuery = leaveQuery.Where(item => item.LeaveDate >= query.FromDate.Value.Date);
        }

        if (query.ToDate.HasValue)
        {
            leaveQuery = leaveQuery.Where(item => item.LeaveDate <= query.ToDate.Value.Date);
        }

        leaveQuery = ApplySorting(leaveQuery, query);
        var totalCount = await leaveQuery.CountAsync();
        var leaveRequests = await leaveQuery
            .OrderByDescending(item => item.LeaveDate)
            .ThenByDescending(item => item.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return ApiOk(ToPagedResult(await MapLeaveRequestsAsync(leaveRequests), query.Page, query.PageSize, totalCount), "Staff leave requests fetched successfully.");
    }

    /// <summary>
    /// Creates a new leave request with Pending status.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponse<LeaveRequestDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponse<LeaveRequestDto>>> Create(CreateLeaveRequestDto request)
    {
        if (!CanAccessStaff(request.StaffId))
        {
            return ApiForbidden("You are not allowed to create leave for another staff member.");
        }

        var staff = await context.Staffs.FirstOrDefaultAsync(item => item.Id == request.StaffId && item.IsActive);
        if (staff is null)
        {
            return ApiNotFound("Active staff not found.");
        }

        if (request.LeaveDate.Date.DayOfWeek == DayOfWeek.Sunday)
        {
            return ApiBadRequest("Leave cannot be requested for Sundays.");
        }

        var duplicateExists = await context.LeaveRequests.AnyAsync(item =>
            item.StaffId == request.StaffId && item.LeaveDate.Date == request.LeaveDate.Date && item.Status != "Rejected");

        if (duplicateExists)
        {
            return ApiBadRequest("A leave request already exists for the selected date.");
        }

        var now = DateTime.UtcNow;
        var leaveRequest = new LeaveRequest
        {
            StaffId = request.StaffId,
            LeaveDate = request.LeaveDate.Date,
            Reason = request.Reason.Trim(),
            Status = "Pending",
            CreatedAt = now,
            UpdatedAt = now
        };

        context.LeaveRequests.Add(leaveRequest);
        await context.SaveChangesAsync();
        await context.Entry(leaveRequest).Reference(item => item.Staff).LoadAsync();

        var response = await MapLeaveRequestAsync(leaveRequest);
        return ApiCreatedAtAction(nameof(GetById), new { id = leaveRequest.Id }, response, "Leave request created successfully.");
    }

    /// <summary>
    /// Updates a pending leave request.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponse<LeaveRequestDto>>> Update(int id, UpdateLeaveRequestDto request)
    {
        var leaveRequest = await context.LeaveRequests
            .Include(item => item.Staff)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (leaveRequest is null)
        {
            return ApiNotFound("Leave request not found.");
        }

        if (!CanAccessStaff(leaveRequest.StaffId))
        {
            return ApiForbidden("You are not allowed to update another staff member's leave request.");
        }

        if (!string.Equals(leaveRequest.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return ApiBadRequest("Only pending leave requests can be updated.");
        }

        if (request.LeaveDate.Date.DayOfWeek == DayOfWeek.Sunday)
        {
            return ApiBadRequest("Leave cannot be requested for Sundays.");
        }

        var duplicateExists = await context.LeaveRequests.AnyAsync(item =>
            item.Id != id && item.StaffId == leaveRequest.StaffId && item.LeaveDate.Date == request.LeaveDate.Date && item.Status != "Rejected");

        if (duplicateExists)
        {
            return ApiBadRequest("A leave request already exists for the selected date.");
        }

        leaveRequest.LeaveDate = request.LeaveDate.Date;
        leaveRequest.Reason = request.Reason.Trim();
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return ApiOk(await MapLeaveRequestAsync(leaveRequest), "Leave request updated successfully.");
    }

    /// <summary>
    /// Approves a pending leave request.
    /// </summary>
    [HttpPatch("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResponse<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponse<LeaveRequestDto>>> Approve(int id, LeaveActionRequestDto request)
    {
        return await UpdateStatusAsync(id, "Approved", request.Remarks);
    }

    /// <summary>
    /// Rejects a pending leave request.
    /// </summary>
    [HttpPatch("{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResponse<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponse<LeaveRequestDto>>> Reject(int id, LeaveActionRequestDto request)
    {
        return await UpdateStatusAsync(id, "Rejected", request.Remarks);
    }

    /// <summary>
    /// Deletes a leave request. Staff can delete only their own pending requests; admin can delete any request.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponse<object>>> Delete(int id)
    {
        var leaveRequest = await context.LeaveRequests.FirstOrDefaultAsync(item => item.Id == id);
        if (leaveRequest is null)
        {
            return ApiNotFound("Leave request not found.");
        }

        if (!CanAccessStaff(leaveRequest.StaffId))
        {
            return ApiForbidden("You are not allowed to delete another staff member's leave request.");
        }

        if (!IsAdmin() && !string.Equals(leaveRequest.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return ApiBadRequest("Only pending leave requests can be deleted by staff.");
        }

        context.LeaveRequests.Remove(leaveRequest);
        await context.SaveChangesAsync();
        return ApiDeleted("Leave request deleted successfully.");
    }

    private async Task<ActionResult<ApiSuccessResponse<LeaveRequestDto>>> UpdateStatusAsync(int id, string status, string? remarks)
    {
        var leaveRequest = await context.LeaveRequests
            .Include(item => item.Staff)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (leaveRequest is null)
        {
            return ApiNotFound("Leave request not found.");
        }

        if (!string.Equals(leaveRequest.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return ApiBadRequest("Only pending leave requests can be approved or rejected.");
        }

        leaveRequest.Status = status;
        leaveRequest.ApprovedBy = GetCurrentUserId();
        leaveRequest.ActionRemarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return ApiOk(await MapLeaveRequestAsync(leaveRequest), $"Leave request {status.ToLowerInvariant()} successfully.");
    }

    private static IQueryable<LeaveRequest> ApplySorting(IQueryable<LeaveRequest> query, LeaveRequestListQuery request)
    {
        var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return request.SortBy?.ToLowerInvariant() switch
        {
            "leavedate" => descending ? query.OrderByDescending(item => item.LeaveDate).ThenByDescending(item => item.Id) : query.OrderBy(item => item.LeaveDate).ThenBy(item => item.Id),
            "status" => descending ? query.OrderByDescending(item => item.Status) : query.OrderBy(item => item.Status),
            "createdat" => descending ? query.OrderByDescending(item => item.CreatedAt) : query.OrderBy(item => item.CreatedAt),
            _ => descending ? query.OrderByDescending(item => item.LeaveDate).ThenByDescending(item => item.Id) : query.OrderBy(item => item.LeaveDate).ThenBy(item => item.Id)
        };
    }

    private bool IsAdmin()
    {
        return string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
    }

    private int? GetCurrentStaffId()
    {
        return int.TryParse(User.FindFirstValue("staffId"), out var staffId) ? staffId : null;
    }

    private bool CanAccessStaff(int staffId)
    {
        return IsAdmin() || GetCurrentStaffId() == staffId;
    }

    private async Task<List<LeaveRequestDto>> MapLeaveRequestsAsync(List<LeaveRequest> leaveRequests)
    {
        var approverIds = leaveRequests
            .Where(item => item.ApprovedBy.HasValue)
            .Select(item => item.ApprovedBy!.Value)
            .Distinct()
            .ToList();

        var approverLookup = approverIds.Count == 0
            ? new Dictionary<int, string>()
            : await context.Users
                .Where(item => approverIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Username);

        return leaveRequests.Select(item => MapLeaveRequest(item, approverLookup)).ToList();
    }

    private async Task<LeaveRequestDto> MapLeaveRequestAsync(LeaveRequest leaveRequest)
    {
        Dictionary<int, string> approverLookup = new();
        if (leaveRequest.ApprovedBy.HasValue)
        {
            approverLookup = await context.Users
                .Where(item => item.Id == leaveRequest.ApprovedBy.Value)
                .ToDictionaryAsync(item => item.Id, item => item.Username);
        }

        return MapLeaveRequest(leaveRequest, approverLookup);
    }

    private static LeaveRequestDto MapLeaveRequest(LeaveRequest leaveRequest, IReadOnlyDictionary<int, string> approverLookup)
    {
        return new LeaveRequestDto
        {
            Id = leaveRequest.Id,
            StaffId = leaveRequest.StaffId,
            EmployeeId = leaveRequest.Staff?.EmployeeId ?? string.Empty,
            FullName = leaveRequest.Staff?.FullName ?? string.Empty,
            LeaveDate = leaveRequest.LeaveDate,
            Reason = leaveRequest.Reason,
            Status = leaveRequest.Status,
            ApprovedBy = leaveRequest.ApprovedBy,
            ApprovedByUsername = leaveRequest.ApprovedBy.HasValue && approverLookup.TryGetValue(leaveRequest.ApprovedBy.Value, out var username)
                ? username
                : string.Empty,
            ActionRemarks = leaveRequest.ActionRemarks ?? string.Empty,
            CreatedAt = leaveRequest.CreatedAt,
            UpdatedAt = leaveRequest.UpdatedAt
        };
    }
}