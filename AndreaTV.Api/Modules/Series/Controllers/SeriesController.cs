using AndreaTV.Api.Infrastructure.Xtream;
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

    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<SeriesCategory>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<SeriesCategory>>> GetCategories(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await seriesService.GetCategoriesAsync(cancellationToken));
        }
        catch (XtreamClientException exception)
        {
            return XtreamProblemDetails.From(exception);
        }
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SeriesCatalogItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<SeriesCatalogItem>>> GetSeries(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await seriesService.GetSeriesAsync(cancellationToken));
        }
        catch (XtreamClientException exception)
        {
            return XtreamProblemDetails.From(exception);
        }
    }
}
