using AndreaTV.Api.Modules.Series.Models;
using AndreaTV.Api.Modules.Series.Services;
using Microsoft.AspNetCore.Mvc;

namespace AndreaTV.Api.Modules.Series.Controllers;

[ApiController]
[Route("api/series")]
public sealed class SeriesController(ISeriesService seriesService) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<SeriesStatus>(StatusCodes.Status200OK)]
    public ActionResult<SeriesStatus> GetStatus() => Ok(seriesService.GetStatus());
}
