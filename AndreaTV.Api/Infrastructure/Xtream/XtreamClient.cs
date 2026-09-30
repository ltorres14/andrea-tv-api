using System.Net;
using System.Text.Json;
using AndreaTV.Api.Configuration;
using AndreaTV.Api.Infrastructure.Xtream.Models;
using Microsoft.Extensions.Options;

namespace AndreaTV.Api.Infrastructure.Xtream;

public sealed class XtreamClient(HttpClient httpClient, IOptions<XtreamOptions> options) : IXtreamClient
{
    private const int DefaultTimeoutSeconds = 30;
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<XtreamAccountInfo> GetAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        var account = await GetJsonAsync<XtreamAccountInfo>(null, cancellationToken);
        return account.UserInfo is not null
            ? account
            : throw Error(
                XtreamClientError.InvalidResponse,
                "The Xtream provider response did not contain account information.");
    }

    public Task<IReadOnlyList<XtreamCategory>> GetVodCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<XtreamCategory>("get_vod_categories", cancellationToken);

    public Task<IReadOnlyList<XtreamVodStream>> GetVodStreamsAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<XtreamVodStream>("get_vod_streams", cancellationToken);

    public Task<IReadOnlyList<XtreamCategory>> GetSeriesCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<XtreamCategory>("get_series_categories", cancellationToken);

    public Task<IReadOnlyList<XtreamSeries>> GetSeriesAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<XtreamSeries>("get_series", cancellationToken);

    public async Task<Uri> ResolveMoviePlaybackUrlAsync(
        int streamId,
        string containerExtension,
        CancellationToken cancellationToken = default)
    {
        if (streamId <= 0 || !IsValidExtension(containerExtension))
        {
            throw new XtreamClientException(
                XtreamClientError.InvalidRequest,
                "A positive stream id and a valid container extension are required.");
        }

        var currentOptions = options.Value;
        var endpoint = BuildMovieUri(currentOptions, streamId, containerExtension);
        return await ExecuteWithTimeoutAsync(currentOptions, cancellationToken, async token =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.UserAgent.ParseAdd("IPTVSmartersPlayer");
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw Error(XtreamClientError.CredentialsRejected, "The Xtream provider rejected the configured credentials.", response.StatusCode);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw Error(XtreamClientError.NotFound, "The requested movie was not found by the provider.", response.StatusCode);
            }

            if (response.StatusCode != HttpStatusCode.Found)
            {
                throw Error(XtreamClientError.UnexpectedResponse, "The Xtream provider did not return the expected playback redirect.", response.StatusCode);
            }

            if (!response.Headers.TryGetValues("Location", out var locations))
            {
                throw Error(XtreamClientError.MissingLocation, "The Xtream playback redirect did not include a Location header.");
            }

            var location = locations.FirstOrDefault();
            if (!Uri.TryCreate(location, UriKind.Absolute, out var resolvedUri)
                || (resolvedUri.Scheme != Uri.UriSchemeHttp && resolvedUri.Scheme != Uri.UriSchemeHttps))
            {
                throw Error(XtreamClientError.InvalidLocation, "The Xtream playback redirect Location is not a valid absolute HTTP URL.");
            }

            return resolvedUri;
        });
    }

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string action, CancellationToken cancellationToken) =>
        await GetJsonAsync<List<T>>(action, cancellationToken);

    private async Task<T> GetJsonAsync<T>(string? action, CancellationToken cancellationToken)
    {
        var currentOptions = options.Value;
        var endpoint = BuildApiUri(currentOptions, action);
        return await ExecuteWithTimeoutAsync(currentOptions, cancellationToken, async token =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw Error(XtreamClientError.CredentialsRejected, "The Xtream provider rejected the configured credentials.", response.StatusCode);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw Error(XtreamClientError.HttpError, "The Xtream provider returned an unsuccessful HTTP response.", response.StatusCode);
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(token);
            try
            {
                return await JsonSerializer.DeserializeAsync<T>(responseStream, SerializerOptions, token)
                    ?? throw Error(XtreamClientError.InvalidResponse, "The Xtream provider returned an empty response.");
            }
            catch (JsonException exception)
            {
                throw new XtreamClientException(
                    XtreamClientError.InvalidResponse,
                    "The Xtream provider returned invalid JSON.",
                    innerException: exception);
            }
        });
    }

    private static async Task<T> ExecuteWithTimeoutAsync<T>(
        XtreamOptions currentOptions,
        CancellationToken cancellationToken,
        Func<CancellationToken, Task<T>> operation)
    {
        var timeoutSeconds = currentOptions.TimeoutSeconds > 0 ? currentOptions.TimeoutSeconds : DefaultTimeoutSeconds;
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            return await operation(timeoutSource.Token);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new XtreamClientException(
                XtreamClientError.Timeout,
                "The Xtream provider did not respond before the configured timeout.",
                innerException: exception);
        }
        catch (HttpRequestException exception)
        {
            throw new XtreamClientException(
                XtreamClientError.Unavailable,
                "The Xtream provider is unavailable.",
                innerException: exception);
        }
    }

    private static Uri BuildApiUri(XtreamOptions currentOptions, string? action)
    {
        var baseUri = ValidateConfiguration(currentOptions);
        var query = $"username={Uri.EscapeDataString(currentOptions.Username)}&password={Uri.EscapeDataString(currentOptions.Password)}";
        if (action is not null)
        {
            query += $"&action={Uri.EscapeDataString(action)}";
        }

        return new UriBuilder(baseUri)
        {
            Path = $"{baseUri.AbsolutePath.TrimEnd('/')}/player_api.php",
            Query = query
        }.Uri;
    }

    private static Uri BuildMovieUri(XtreamOptions currentOptions, int streamId, string containerExtension)
    {
        var baseUri = ValidateConfiguration(currentOptions);
        return new UriBuilder(baseUri)
        {
            Path = $"{baseUri.AbsolutePath.TrimEnd('/')}/movie/{Uri.EscapeDataString(currentOptions.Username)}/"
                + $"{Uri.EscapeDataString(currentOptions.Password)}/{streamId}.{containerExtension.TrimStart('.')}"
        }.Uri;
    }

    private static Uri ValidateConfiguration(XtreamOptions currentOptions)
    {
        if (string.IsNullOrWhiteSpace(currentOptions.BaseUrl)
            || string.IsNullOrWhiteSpace(currentOptions.Username)
            || string.IsNullOrWhiteSpace(currentOptions.Password))
        {
            throw Error(XtreamClientError.Configuration, "Xtream configuration is incomplete.");
        }

        if (!Uri.TryCreate(currentOptions.BaseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(baseUri.UserInfo))
        {
            throw Error(XtreamClientError.Configuration, "Xtream BaseUrl must be an absolute HTTP or HTTPS URL without embedded credentials.");
        }

        return baseUri;
    }

    private static bool IsValidExtension(string extension)
    {
        var value = extension?.TrimStart('.');
        return !string.IsNullOrWhiteSpace(value)
            && value.Length <= 10
            && value.All(char.IsAsciiLetterOrDigit);
    }

    private static XtreamClientException Error(
        XtreamClientError error,
        string message,
        HttpStatusCode? providerStatusCode = null) => new(error, message, providerStatusCode);
}
