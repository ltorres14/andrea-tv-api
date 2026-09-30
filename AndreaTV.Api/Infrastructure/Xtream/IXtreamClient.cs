using AndreaTV.Api.Infrastructure.Xtream.Models;

namespace AndreaTV.Api.Infrastructure.Xtream;

/// <summary>
/// Defines the boundary for future communication with an Xtream-compatible provider.
/// </summary>
public interface IXtreamClient
{
    Task<XtreamAccountInfo> GetAccountInfoAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<XtreamCategory>> GetVodCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<XtreamVodStream>> GetVodStreamsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<XtreamCategory>> GetSeriesCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<XtreamSeries>> GetSeriesAsync(CancellationToken cancellationToken = default);
    Task<Uri> ResolveMoviePlaybackUrlAsync(int streamId, string containerExtension, CancellationToken cancellationToken = default);
}
