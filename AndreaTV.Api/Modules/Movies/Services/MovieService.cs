using AndreaTV.Api.Modules.Movies.Models;

namespace AndreaTV.Api.Modules.Movies.Services;

public sealed class MovieService : IMovieService
{
    public MovieStatus GetStatus() => new("Movies module is ready.");
}
