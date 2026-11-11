using Microsoft.AspNetCore.Mvc;

namespace GradebookAPI.Controllers;

/// <summary>Abstract controller base with status code shorthands.</summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    public IActionResult InternalServerError(object? value)
    {
        return StatusCode(StatusCodes.Status500InternalServerError, value);
    }

    public IActionResult Forbid(object? value)
    {
        return StatusCode(StatusCodes.Status403Forbidden, value);
    }
}