using AndreaTV.Api.Modules.Movies.Models;

namespace AndreaTV.Api.Modules.Movies.Services;

public interface IMovieService
{
    MovieStatus GetStatus();
    Task<IReadOnlyList<MovieCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<MovieCatalogPage> GetMoviesAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MoviePlayback> ResolvePlaybackAsync(int streamId, string containerExtension, CancellationToken cancellationToken = default);
}
