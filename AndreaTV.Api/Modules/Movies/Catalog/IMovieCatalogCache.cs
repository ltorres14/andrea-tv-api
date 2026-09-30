namespace AndreaTV.Api.Modules.Movies.Catalog;

public interface IMovieCatalogCache
{
    Task<MovieCatalogSnapshot> GetAsync(CancellationToken cancellationToken = default);
}
