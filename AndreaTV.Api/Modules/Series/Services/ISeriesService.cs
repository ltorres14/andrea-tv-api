using AndreaTV.Api.Modules.Series.Models;

namespace AndreaTV.Api.Modules.Series.Services;

public interface ISeriesService
{
    SeriesStatus GetStatus();
    Task<IReadOnlyList<SeriesCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SeriesCatalogItem>> GetSeriesAsync(CancellationToken cancellationToken = default);
}
