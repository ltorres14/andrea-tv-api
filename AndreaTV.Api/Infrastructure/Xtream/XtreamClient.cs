using System.Net;
using System.Text.Json;
using AndreaTV.Api.Configuration;
using AndreaTV.Api.Infrastructure.Xtream.Models;
using Microsoft.Extensions.Options;

namespace AndreaTV.Api.Infrastructure.Xtream;

public sealed class XtreamClient(
    HttpClient httpClient,
    IOptions<XtreamOptions> options) : IXtreamClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<XtreamAccountInfo> GetAccountInfoAsync(
        CancellationToken cancellationToken = default)
    {
        var currentOptions = options.Value;
        var endpoint = BuildAccountInfoUri(currentOptions);
        var timeoutSeconds = currentOptions.TimeoutSeconds > 0
            ? currentOptions.TimeoutSeconds
            : 30;

        using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutTokenSource.Token);

            if (!response.IsSuccessStatusCode)
            {
                throw new XtreamClientException(
                    XtreamClientError.HttpError,
                    "The Xtream provider returned an unsuccessful HTTP response.",
                    response.StatusCode);
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(
                timeoutTokenSource.Token);

            XtreamAccountInfo? accountInfo;
            try
            {
                accountInfo = await JsonSerializer.DeserializeAsync<XtreamAccountInfo>(
                    responseStream,
                    SerializerOptions,
                    timeoutTokenSource.Token);
            }
            catch (JsonException exception)
            {
                throw new XtreamClientException(
                    XtreamClientError.InvalidResponse,
                    "The Xtream provider returned invalid JSON.",
                    innerException: exception);
            }

            if (accountInfo?.UserInfo is null)
            {
                throw new XtreamClientException(
                    XtreamClientError.InvalidResponse,
                    "The Xtream provider response did not contain account information.");
            }

            return accountInfo;
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
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

    private static Uri BuildAccountInfoUri(XtreamOptions currentOptions)
    {
        if (string.IsNullOrWhiteSpace(currentOptions.BaseUrl)
            || string.IsNullOrWhiteSpace(currentOptions.Username)
            || string.IsNullOrWhiteSpace(currentOptions.Password))
        {
            throw new XtreamClientException(
                XtreamClientError.Configuration,
                "Xtream configuration is incomplete.");
        }

        if (!Uri.TryCreate(currentOptions.BaseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp
                && baseUri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(baseUri.UserInfo))
        {
            throw new XtreamClientException(
                XtreamClientError.Configuration,
                "Xtream BaseUrl must be an absolute HTTP or HTTPS URL without embedded credentials.");
        }

        var uriBuilder = new UriBuilder(baseUri)
        {
            Path = $"{baseUri.AbsolutePath.TrimEnd('/')}/player_api.php",
            Query = $"username={Uri.EscapeDataString(currentOptions.Username)}"
                + $"&password={Uri.EscapeDataString(currentOptions.Password)}"
        };

        return uriBuilder.Uri;
    }
}
