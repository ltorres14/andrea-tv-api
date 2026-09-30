namespace AndreaTV.Api.Configuration;

public sealed class MovieCatalogOptions
{
    public const string SectionName = "MovieCatalog";

    public string StoragePath { get; init; } = "App_Data/alltvpremiere-movie-catalog.json";

    public int RefreshHours { get; init; } = 24;
}
