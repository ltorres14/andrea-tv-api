using AndreaTV.Api.Infrastructure.Xtream;
using AndreaTV.Api.Modules.Series.Models;

namespace AndreaTV.Api.Modules.Series.Services;

public sealed class SeriesService(IXtreamClient xtreamClient) : ISeriesService
{
    public SeriesStatus GetStatus() => new("Series module is ready.");

    public async Task<IReadOnlyList<SeriesCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        (await xtreamClient.GetSeriesCategoriesAsync(cancellationToken))
            .Select(category => new SeriesCategory(category.CategoryId, category.CategoryName))
            .ToArray();

    public async Task<IReadOnlyList<SeriesCatalogItem>> GetSeriesAsync(CancellationToken cancellationToken = default) =>
        (await xtreamClient.GetSeriesAsync(cancellationToken))
            .Select(series => new SeriesCatalogItem(
                series.SeriesId,
                series.Name,
                series.CategoryId,
                series.Cover,
                series.Rating,
                series.Plot,
                series.ReleaseDate,
                series.Genre,
                series.LastModified))
            .ToArray();
}
