using Microsoft.AspNetCore.Mvc;

namespace GradebookAPI.Controllers;

/// <summary>The abstract controller base for all V1 API endpoints.</summary>
[Route("v1/[controller]/[action]")]
public abstract class V1ApiController : ApiControllerBase;