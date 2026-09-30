using AndreaTV.Api.Infrastructure.Xtream;
using AndreaTV.Api.Infrastructure.Xtream.Models;
using AndreaTV.Api.Modules.Devices.Models;

namespace AndreaTV.Api.Modules.Devices.Services;

public sealed class XtreamConnectionService(IXtreamClient xtreamClient)
    : IXtreamConnectionService
{
    public async Task<XtreamConnectionStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var accountInfo = await xtreamClient.GetAccountInfoAsync(cancellationToken);
        var userInfo = accountInfo.UserInfo!;
        var expirationDate = ToDateTimeOffset(userInfo.ExpirationTimestamp);
        var authenticated = userInfo.Authenticated == true;
        var active = string.Equals(
            userInfo.Status,
            "Active",
            StringComparison.OrdinalIgnoreCase);
        var expired = expirationDate.HasValue
            && expirationDate.Value <= DateTimeOffset.UtcNow;

        return new XtreamConnectionStatus
        {
            Connected = true,
            Authenticated = authenticated,
            Status = userInfo.Status
                ?? (authenticated ? "Unknown" : "Rejected"),
            IsTrial = userInfo.IsTrial == true,
            ExpirationDate = expirationDate,
            ActiveConnections = userInfo.ActiveConnections,
            MaxConnections = userInfo.MaxConnections,
            AllowedOutputFormats = userInfo.AllowedOutputFormats ?? [],
            Server = MapServer(accountInfo.ServerInfo),
            IsUsable = authenticated && active && !expired
        };
    }

    private static XtreamServerStatus? MapServer(XtreamServerInfo? serverInfo)
    {
        if (serverInfo is null)
        {
            return null;
        }

        return new XtreamServerStatus
        {
            Protocol = serverInfo.Protocol,
            Timezone = serverInfo.Timezone,
            CurrentTime = ToDateTimeOffset(serverInfo.CurrentTimestamp)
        };
    }

    private static DateTimeOffset? ToDateTimeOffset(long? unixTimestamp)
    {
        if (!unixTimestamp.HasValue)
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
