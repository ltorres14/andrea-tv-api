using System.Text.Json.Serialization;

namespace AndreaTV.Api.Modules.Series.Models;

public sealed record SeriesCategory(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CategoryId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name);

public sealed record SeriesCatalogItem(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? SeriesId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CategoryId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Cover,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Rating,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Plot,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ReleaseDate,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Genre,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? LastModified);
