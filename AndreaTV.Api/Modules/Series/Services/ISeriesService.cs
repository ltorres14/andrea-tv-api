using AndreaTV.Api.Modules.Series.Models;

namespace AndreaTV.Api.Modules.Series.Services;

public interface ISeriesService
{
    SeriesStatus GetStatus();
}
