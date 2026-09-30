using System.Text.Json.Serialization;

namespace AndreaTV.Api.Modules.Movies.Models;

public sealed record MovieCategory(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CategoryId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name);

public sealed record MovieCatalogItem(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? StreamId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CategoryId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? StreamIcon,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Rating,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? Added,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContainerExtension);

public sealed record MovieCatalogPage(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<MovieCatalogItem> Items);

public sealed record MoviePlayback(int StreamId, string Url);
