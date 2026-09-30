using AndreaTV.Api.Modules.Movies.Models;

namespace AndreaTV.Api.Modules.Movies.Catalog;

public sealed record MovieCatalogSnapshot(
    int Version,
    DateTimeOffset RefreshedAtUtc,
    IReadOnlyList<MovieCategory> Categories,
    IReadOnlyList<MovieCatalogItem> Movies)
{
    public const int CurrentVersion = 1;
}
