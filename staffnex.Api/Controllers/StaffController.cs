using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staffnex.Api.Data;
using staffnex.Api.DTOs;
using staffnex.Api.Models;

namespace staffnex.Api.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize]
public class StaffController(AppDbContext context) : ApiControllerBase
{
    /// <summary>
    /// Returns all staff records. Admin only.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResponse<PagedResult<StaffDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponse<PagedResult<StaffDto>>>> GetAll([FromQuery] StaffListQuery query)
    {
        var staffQuery = context.Staffs
            .Include(staff => staff.Department)
            .Include(staff => staff.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            staffQuery = staffQuery.Where(staff =>
                staff.EmployeeId.Contains(search) ||
                staff.FullName.Contains(search) ||
                staff.Email.Contains(search) ||
                staff.Phone.Contains(search) ||
                (staff.User != null && staff.User.Username.Contains(search)));
        }

        if (query.DepartmentId.HasValue)
        {
            staffQuery = staffQuery.Where(staff => staff.DepartmentId == query.DepartmentId.Value);
        }

        if (query.IsActive.HasValue)
        {
            staffQuery = staffQuery.Where(staff => staff.IsActive == query.IsActive.Value);
        }

        staffQuery = ApplySorting(staffQuery, query);

        var totalCount = await staffQuery.CountAsync();
        var staffList = await staffQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return ApiOk(ToPagedResult(staffList.Select(MapStaffDto).ToList(), query.Page, query.PageSize, totalCount), "Staff list fetched successfully.");
    }

    /// <summary>
    /// Returns a single staff profile. Staff users can only access their own profile.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<StaffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiSuccessResponse<StaffDto>>> GetById(int id)
    {
        if (!CanAccessStaff(id))
        {
            return ApiForbidden("You are not allowed to access another staff profile.");
        }

        var staff = await context.Staffs
            .Include(item => item.Department)
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (staff is null)
        {
            return ApiNotFound("Staff record not found.");
        }

        return ApiOk(MapStaffDto(staff), "Staff profile fetched successfully.");
    }

    /// <summary>
    /// Returns the department master list. Admin only.
    /// </summary>
    [HttpGet("departments")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiSuccessResponse<IEnumerable<Department>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponse<IEnumerable<Department>>>> GetDepartments()
    {
        var departments = await context.Departments
            .OrderBy(department => department.Name)
            .ToListAsync();

        return ApiOk<IEnumerable<Department>>(departments, "Departments fetched successfully.");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiSuccessResponse<StaffDto>>> Create(CreateStaffRequest request)
    {
        if (request.DepartmentId.HasValue)
        {
            var departmentExists = await context.Departments.AnyAsync(item => item.Id == request.DepartmentId.Value);
            if (!departmentExists)
            {
                return ApiBadRequest("Department not found.");
            }
        }

        var usernameExists = await context.Users.AnyAsync(user => user.Username == request.Username);
        if (usernameExists)
        {
            return ApiBadRequest("Username already exists.");
        }

        var employeeId = await GenerateEmployeeIdAsync();
        var now = DateTime.UtcNow;

        var staff = new Staff
        {
            EmployeeId = employeeId,
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            Designation = request.Designation,
            DepartmentId = request.DepartmentId,
            MonthlySalary = request.MonthlySalary,
            JoinDate = request.JoinDate.Date,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Staffs.Add(staff);
        await context.SaveChangesAsync();

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Staff",
            StaffId = staff.Id,
            IsActive = true,
            CreatedAt = now
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        await context.Entry(staff).Reference(item => item.Department).LoadAsync();
        await context.Entry(staff).Reference(item => item.User).LoadAsync();

        return ApiCreatedAtAction(nameof(GetById), new { id = staff.Id }, MapStaffDto(staff), "Staff created successfully.");
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiSuccessResponse<StaffDto>>> Update(int id, UpdateStaffRequest request)
    {
        var staff = await context.Staffs
            .Include(item => item.Department)
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (staff is null)
        {
            return ApiNotFound("Staff record not found.");
        }

        if (request.DepartmentId.HasValue)
        {
            var departmentExists = await context.Departments.AnyAsync(item => item.Id == request.DepartmentId.Value);
            if (!departmentExists)
            {
                return ApiBadRequest("Department not found.");
            }
        }

        var usernameExists = await context.Users.AnyAsync(user => user.Username == request.Username && user.Id != staff.User!.Id);
        if (usernameExists)
        {
            return ApiBadRequest("Username already exists.");
        }

        staff.FullName = request.FullName;
        staff.Phone = request.Phone;
        staff.Email = request.Email;
        staff.Designation = request.Designation;
        staff.DepartmentId = request.DepartmentId;
        staff.MonthlySalary = request.MonthlySalary;
        staff.JoinDate = request.JoinDate.Date;
        staff.IsActive = request.IsActive;
        staff.UpdatedAt = DateTime.UtcNow;

        if (staff.User is not null)
        {
            staff.User.Username = request.Username;
            staff.User.IsActive = request.IsActive;

            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                staff.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            }
        }

        await context.SaveChangesAsync();
        await context.Entry(staff).Reference(item => item.Department).LoadAsync();

        return ApiOk(MapStaffDto(staff), "Staff updated successfully.");
    }

    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiSuccessResponse<StaffDto>>> ToggleActive(int id)
    {
        var staff = await context.Staffs
            .Include(item => item.Department)
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (staff is null)
        {
            return ApiNotFound("Staff record not found.");
        }

        staff.IsActive = !staff.IsActive;
        staff.UpdatedAt = DateTime.UtcNow;

        if (staff.User is not null)
        {
            staff.User.IsActive = staff.IsActive;
        }

        await context.SaveChangesAsync();
        return ApiOk(MapStaffDto(staff), "Staff status updated successfully.");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiSuccessResponse<object>>> Delete(int id)
    {
        var staff = await context.Staffs.FirstOrDefaultAsync(item => item.Id == id);
        if (staff is null)
        {
            return ApiNotFound("Staff record not found.");
        }

        context.Staffs.Remove(staff);
        await context.SaveChangesAsync();
        return ApiDeleted("Staff deleted successfully.");
    }

    private static IQueryable<Staff> ApplySorting(IQueryable<Staff> query, StaffListQuery request)
    {
        var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return request.SortBy?.ToLowerInvariant() switch
        {
            "employeeid" => descending ? query.OrderByDescending(staff => staff.EmployeeId) : query.OrderBy(staff => staff.EmployeeId),
            "fullname" => descending ? query.OrderByDescending(staff => staff.FullName) : query.OrderBy(staff => staff.FullName),
            "designation" => descending ? query.OrderByDescending(staff => staff.Designation) : query.OrderBy(staff => staff.Designation),
            "monthlysalary" => descending ? query.OrderByDescending(staff => staff.MonthlySalary) : query.OrderBy(staff => staff.MonthlySalary),
            "joindate" => descending ? query.OrderByDescending(staff => staff.JoinDate) : query.OrderBy(staff => staff.JoinDate),
            _ => descending ? query.OrderByDescending(staff => staff.Id) : query.OrderBy(staff => staff.Id)
        };
    }

    private async Task<string> GenerateEmployeeIdAsync()
    {
        var employeeIds = await context.Staffs
            .Select(staff => staff.EmployeeId)
            .ToListAsync();

        var nextNumber = employeeIds
            .Where(value => value.StartsWith("EMP", StringComparison.OrdinalIgnoreCase) && value.Length > 3)
            .Select(value => int.TryParse(value[3..], out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"EMP{nextNumber:000}";
    }

    private static StaffDto MapStaffDto(Staff staff)
    {
        return new StaffDto
        {
            Id = staff.Id,
            EmployeeId = staff.EmployeeId,
            FullName = staff.FullName,
            Phone = staff.Phone,
            Email = staff.Email,
            Designation = staff.Designation,
            DepartmentId = staff.DepartmentId,
            DepartmentName = staff.Department?.Name ?? string.Empty,
            MonthlySalary = staff.MonthlySalary,
            JoinDate = staff.JoinDate,
            IsActive = staff.IsActive,
            CreatedAt = staff.CreatedAt,
            UpdatedAt = staff.UpdatedAt,
            Username = staff.User?.Username ?? string.Empty
        };
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