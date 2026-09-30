using AndreaTV.Api.Infrastructure.Xtream.Models;

namespace AndreaTV.Api.Infrastructure.Xtream;

/// <summary>
/// Defines the boundary for future communication with an Xtream-compatible provider.
/// </summary>
public interface IXtreamClient
{
    Task<XtreamAccountInfo> GetAccountInfoAsync(
        CancellationToken cancellationToken = default);
}
