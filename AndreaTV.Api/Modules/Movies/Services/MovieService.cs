using AndreaTV.Api.Infrastructure.Xtream;
using AndreaTV.Api.Modules.Movies.Catalog;
using AndreaTV.Api.Modules.Movies.Models;

namespace AndreaTV.Api.Modules.Movies.Services;

public sealed class MovieService(
    IMovieCatalogCache catalogCache,
    IXtreamClient xtreamClient) : IMovieService
{
    public MovieStatus GetStatus() => new("Movies module is ready.");

    public async Task<IReadOnlyList<MovieCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        (await catalogCache.GetAsync(cancellationToken)).Categories;

    public async Task<MovieCatalogPage> GetMoviesAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100);

        var movies = (await catalogCache.GetAsync(cancellationToken)).Movies;

        var totalItems = movies.Count;
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var offset = (long)(page - 1) * pageSize;
        var items = offset >= totalItems
            ? []
            : movies.Skip((int)offset).Take(pageSize).ToArray();

        return new MovieCatalogPage(page, pageSize, totalItems, totalPages, items);
    }

    public async Task<MoviePlayback> ResolvePlaybackAsync(
        int streamId,
        string containerExtension,
        CancellationToken cancellationToken = default)
    {
        var url = await xtreamClient.ResolveMoviePlaybackUrlAsync(streamId, containerExtension, cancellationToken);
        return new MoviePlayback(streamId, url.AbsoluteUri);
    }
}
