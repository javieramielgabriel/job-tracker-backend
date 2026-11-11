using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Controllers;

/// <summary>
/// Base API controller applying common MVC API conventions and JSON content negotiation.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase;
