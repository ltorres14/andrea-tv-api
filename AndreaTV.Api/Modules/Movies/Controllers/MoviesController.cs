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
}
