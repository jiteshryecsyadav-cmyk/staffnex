using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using staffnex.Api.DTOs;

namespace staffnex.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult ApiBadRequest(string message, Dictionary<string, string[]>? errors = null)
    {
        return StatusCode(StatusCodes.Status400BadRequest, CreateError(StatusCodes.Status400BadRequest, "Bad Request", message, errors));
    }

    protected ActionResult ApiUnauthorized(string message)
    {
        return StatusCode(StatusCodes.Status401Unauthorized, CreateError(StatusCodes.Status401Unauthorized, "Unauthorized", message));
    }

    protected ActionResult ApiForbidden(string message)
    {
        return StatusCode(StatusCodes.Status403Forbidden, CreateError(StatusCodes.Status403Forbidden, "Forbidden", message));
    }

    protected ActionResult ApiNotFound(string message)
    {
        return StatusCode(StatusCodes.Status404NotFound, CreateError(StatusCodes.Status404NotFound, "Not Found", message));
    }

    protected OkObjectResult ApiOk<T>(T data, string message = "Request completed successfully.")
    {
        return Ok(CreateSuccess(data, message));
    }

    protected CreatedAtActionResult ApiCreatedAtAction<T>(string actionName, object? routeValues, T data, string message = "Resource created successfully.")
    {
        return CreatedAtAction(actionName, routeValues, CreateSuccess(data, message));
    }

    protected OkObjectResult ApiDeleted(string message = "Resource deleted successfully.")
    {
        return Ok(CreateSuccess<object?>(null, message));
    }

    protected static PagedResult<T> ToPagedResult<T>(IReadOnlyCollection<T> items, int page, int pageSize, int totalCount)
    {
        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static ApiSuccessResponse<T> CreateSuccess<T>(T data, string message)
    {
        return new ApiSuccessResponse<T>
        {
            Data = data,
            Message = message
        };
    }

    private ApiErrorResponse CreateError(int statusCode, string title, string message, Dictionary<string, string[]>? errors = null)
    {
        return new ApiErrorResponse
        {
            StatusCode = statusCode,
            Title = title,
            Message = message,
            TraceId = HttpContext.TraceIdentifier,
            Errors = errors ?? new Dictionary<string, string[]>()
        };
    }
}