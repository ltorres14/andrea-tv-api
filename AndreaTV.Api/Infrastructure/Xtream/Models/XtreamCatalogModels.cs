using System.Text.Json.Serialization;
using AndreaTV.Api.Infrastructure.Xtream.Serialization;

namespace AndreaTV.Api.Infrastructure.Xtream.Models;

public sealed class XtreamCategory
{
    [JsonPropertyName("category_id")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? CategoryId { get; init; }

    [JsonPropertyName("category_name")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? CategoryName { get; init; }
}

public sealed class XtreamVodStream
{
    [JsonPropertyName("stream_id")]
    [JsonConverter(typeof(FlexibleInt32JsonConverter))]
    public int? StreamId { get; init; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Name { get; init; }

    [JsonPropertyName("category_id")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? CategoryId { get; init; }

    [JsonPropertyName("stream_icon")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? StreamIcon { get; init; }

    [JsonPropertyName("rating")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Rating { get; init; }

    [JsonPropertyName("added")]
    [JsonConverter(typeof(FlexibleInt64JsonConverter))]
    public long? Added { get; init; }

    [JsonPropertyName("container_extension")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? ContainerExtension { get; init; }
}

public sealed class XtreamSeries
{
    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleInt32JsonConverter))]
    public int? SeriesId { get; init; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Name { get; init; }

    [JsonPropertyName("category_id")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? CategoryId { get; init; }

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Cover { get; init; }

    [JsonPropertyName("rating")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Rating { get; init; }

    [JsonPropertyName("plot")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Plot { get; init; }

    [JsonPropertyName("releaseDate")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? ReleaseDate { get; init; }

    [JsonPropertyName("genre")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Genre { get; init; }

    [JsonPropertyName("last_modified")]
    [JsonConverter(typeof(FlexibleInt64JsonConverter))]
    public long? LastModified { get; init; }
}
