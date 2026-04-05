using System.ComponentModel.DataAnnotations;

namespace staffnex.Api.DTOs;

public class ApiSuccessResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public class ApiErrorResponse
{
    public int StatusCode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public Dictionary<string, string[]> Errors { get; set; } = new();
}

public class PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(50)]
    public string? SortBy { get; set; }

    [RegularExpression("^(asc|desc)$", ErrorMessage = "SortDirection must be asc or desc.")]
    public string SortDirection { get; set; } = "asc";
}

public class StaffListQuery : PaginationQuery
{
    [StringLength(100)]
    public string? Search { get; set; }

    public int? DepartmentId { get; set; }
    public bool? IsActive { get; set; }
}

public class AttendanceListQuery : PaginationQuery
{
    public int? StaffId { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }
}

public class LeaveRequestListQuery : PaginationQuery
{
    public int? StaffId { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}