using AndreaTV.Api.Modules.Devices.Models;

namespace AndreaTV.Api.Modules.Devices.Services;

public interface IXtreamConnectionService
{
    Task<XtreamConnectionStatus> GetStatusAsync(
        CancellationToken cancellationToken = default);
}
