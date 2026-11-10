using Microsoft.AspNetCore.Mvc;

namespace GradebookAPI.Controllers;

public class GradesController : ApiController
{
    private readonly ILogger<GradesController> _logger;

    public GradesController(
        ILogger<GradesController> logger
    )
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetById()
    {
        return InternalServerError("Test");
    }
}