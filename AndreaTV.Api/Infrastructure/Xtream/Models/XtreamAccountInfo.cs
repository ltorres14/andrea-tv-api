using System.Text.Json.Serialization;
using AndreaTV.Api.Infrastructure.Xtream.Serialization;

namespace AndreaTV.Api.Infrastructure.Xtream.Models;

public sealed class XtreamAccountInfo
{
    [JsonPropertyName("user_info")]
    public XtreamUserInfo? UserInfo { get; init; }

    [JsonPropertyName("server_info")]
    public XtreamServerInfo? ServerInfo { get; init; }
}

public sealed class XtreamUserInfo
{
    [JsonPropertyName("auth")]
    [JsonConverter(typeof(FlexibleBooleanJsonConverter))]
    public bool? Authenticated { get; init; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Status { get; init; }

    [JsonPropertyName("exp_date")]
    [JsonConverter(typeof(FlexibleInt64JsonConverter))]
    public long? ExpirationTimestamp { get; init; }

    [JsonPropertyName("is_trial")]
    [JsonConverter(typeof(FlexibleBooleanJsonConverter))]
    public bool? IsTrial { get; init; }

    [JsonPropertyName("active_cons")]
    [JsonConverter(typeof(FlexibleInt32JsonConverter))]
    public int? ActiveConnections { get; init; }

    [JsonPropertyName("max_connections")]
    [JsonConverter(typeof(FlexibleInt32JsonConverter))]
    public int? MaxConnections { get; init; }

    [JsonPropertyName("allowed_output_formats")]
    [JsonConverter(typeof(FlexibleStringListJsonConverter))]
    public IReadOnlyList<string>? AllowedOutputFormats { get; init; }
}

public sealed class XtreamServerInfo
{
    [JsonPropertyName("url")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Url { get; init; }

    [JsonPropertyName("port")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Port { get; init; }

    [JsonPropertyName("https_port")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? HttpsPort { get; init; }

    [JsonPropertyName("server_protocol")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Protocol { get; init; }

    [JsonPropertyName("timezone")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Timezone { get; init; }

    [JsonPropertyName("timestamp_now")]
    [JsonConverter(typeof(FlexibleInt64JsonConverter))]
    public long? CurrentTimestamp { get; init; }

    [JsonPropertyName("time_now")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? CurrentTime { get; init; }
}
