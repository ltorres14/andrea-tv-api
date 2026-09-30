namespace AndreaTV.Api.Modules.Movies.Catalog;

public interface IMovieCatalogRepository
{
    Task<MovieCatalogSnapshot?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(MovieCatalogSnapshot snapshot, CancellationToken cancellationToken = default);
}
