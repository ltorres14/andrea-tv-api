using AndreaTV.Api.Modules.Series.Models;

namespace AndreaTV.Api.Modules.Series.Services;

public sealed class SeriesService : ISeriesService
{
    public SeriesStatus GetStatus() => new("Series module is ready.");
}
