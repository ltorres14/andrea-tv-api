using AndreaTV.Api.Infrastructure.Xtream;
using AndreaTV.Api.Modules.Movies.Models;
using AndreaTV.Api.Modules.Movies.Services;
using Microsoft.AspNetCore.Mvc;

namespace AndreaTV.Api.Modules.Movies.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class MoviesController(IMovieService movieService) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<MovieStatus>(StatusCodes.Status200OK)]
    public ActionResult<MovieStatus> GetStatus() => Ok(movieService.GetStatus());

    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<MovieCategory>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<MovieCategory>>> GetCategories(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await movieService.GetCategoriesAsync(cancellationToken));
        }
        catch (XtreamClientException exception)
        {
            return XtreamProblemDetails.From(exception);
        }
    }

    [HttpGet]
    [ProducesResponseType<MovieCatalogPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<MovieCatalogPage>> GetMovies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            return InvalidPagination("page must be greater than or equal to 1.");
        }

        if (pageSize is < 1 or > 100)
        {
            return InvalidPagination("pageSize must be between 1 and 100.");
        }

        try
        {
            return Ok(await movieService.GetMoviesAsync(page, pageSize, cancellationToken));
        }
        catch (XtreamClientException exception)
        {
            return XtreamProblemDetails.From(exception);
        }
    }

    private static BadRequestObjectResult InvalidPagination(string detail) => new(new ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Invalid pagination parameters",
        Detail = detail
    });

    [HttpGet("{streamId:int}/playback")]
    [ProducesResponseType<MoviePlayback>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<MoviePlayback>> GetPlayback(
        int streamId,
        [FromQuery] string extension,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await movieService.ResolvePlaybackAsync(streamId, extension, cancellationToken));
        }
        catch (XtreamClientException exception)
        {
            return XtreamProblemDetails.From(exception);
        }
    }
}
