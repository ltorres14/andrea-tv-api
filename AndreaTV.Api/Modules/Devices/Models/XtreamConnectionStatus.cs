using System.Text.Json.Serialization;

namespace AndreaTV.Api.Modules.Devices.Models;

public sealed class XtreamConnectionStatus
{
    public bool Connected { get; init; }

    public bool Authenticated { get; init; }

    public string Status { get; init; } = string.Empty;

    public bool IsTrial { get; init; }

    public DateTimeOffset? ExpirationDate { get; init; }

    public int? ActiveConnections { get; init; }

    public int? MaxConnections { get; init; }

    public IReadOnlyList<string> AllowedOutputFormats { get; init; } = [];

    public XtreamServerStatus? Server { get; init; }

    [JsonIgnore]
    public bool IsUsable { get; init; }
}

public sealed class XtreamServerStatus
{
    public string? Protocol { get; init; }

    public string? Timezone { get; init; }

    public DateTimeOffset? CurrentTime { get; init; }
}
