using AndreaTV.Api.Modules.Movies.Models;

namespace AndreaTV.Api.Modules.Movies.Services;

public interface IMovieService
{
    MovieStatus GetStatus();
}
