using System.Net;
using AndreaTV.Api.Configuration;
using AndreaTV.Api.Infrastructure.Xtream;
using AndreaTV.Api.Modules.Devices.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AndreaTV.Api.Tests.Infrastructure.Xtream;

public sealed class XtreamClientTests
{
    [Fact]
    public async Task ActiveAccount_IsAuthenticatedAndBuildsExpectedEndpoint()
    {
        const string json = """
            {
              "user_info": {
                "auth": 1,
                "status": "Active",
                "is_trial": 0,
                "active_cons": 1,
                "max_connections": 2,
                "allowed_output_formats": ["m3u8", "ts"]
              },
              "server_info": {
                "server_protocol": "https",
                "timezone": "UTC",
                "timestamp_now": 1893456000
              }
            }
            """;
        var handler = new StubHttpMessageHandler(
            _ => JsonResponse(HttpStatusCode.OK, json));
        var client = CreateClient(
            handler,
            baseUrl: "https://provider.invalid/root",
            username: "demo user",
            password: "p@ss");
        var service = new XtreamConnectionService(client);

        var result = await service.GetStatusAsync();

        Assert.True(result.Connected);
        Assert.True(result.Authenticated);
        Assert.True(result.IsUsable);
        Assert.Equal("Active", result.Status);
        Assert.Equal(["m3u8", "ts"], result.AllowedOutputFormats);
        Assert.Equal("https", result.Server?.Protocol);
        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/root/player_api.php", handler.LastRequestUri.AbsolutePath);
        Assert.Contains("username=demo%20user", handler.LastRequestUri.Query);
        Assert.Contains("password=p%40ss", handler.LastRequestUri.Query);
    }

    [Fact]
    public async Task RejectedCredentials_AreReportedAsUnauthenticated()
    {
        const string json = """
            { "user_info": { "auth": "0", "status": "Disabled" } }
            """;
        var client = CreateClient(new StubHttpMessageHandler(
            _ => JsonResponse(HttpStatusCode.OK, json)));
        var service = new XtreamConnectionService(client);

        var result = await service.GetStatusAsync();

        Assert.True(result.Connected);
        Assert.False(result.Authenticated);
        Assert.False(result.IsUsable);
    }

    [Fact]
    public async Task InactiveOrExpiredAccount_IsNotUsable()
    {
        const string json = """
            {
              "user_info": {
                "auth": "1",
                "status": "Expired",
                "exp_date": "946684800"
              }
            }
            """;
        var client = CreateClient(new StubHttpMessageHandler(
            _ => JsonResponse(HttpStatusCode.OK, json)));
        var service = new XtreamConnectionService(client);

        var result = await service.GetStatusAsync();

        Assert.True(result.Authenticated);
        Assert.False(result.IsUsable);
        Assert.Equal("Expired", result.Status);
        Assert.Equal(
            DateTimeOffset.FromUnixTimeSeconds(946684800),
            result.ExpirationDate);
    }

    [Fact]
    public async Task Timeout_ThrowsSanitizedTimeoutError()
    {
        var handler = new StubHttpMessageHandler(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable");
        });
        var client = CreateClient(handler, timeoutSeconds: 1);

        var exception = await Assert.ThrowsAsync<XtreamClientException>(
            () => client.GetAccountInfoAsync());

        Assert.Equal(XtreamClientError.Timeout, exception.Error);
        Assert.DoesNotContain("test-password", exception.Message);
    }

    [Fact]
    public async Task InvalidJson_ThrowsInvalidResponseError()
    {
        var client = CreateClient(new StubHttpMessageHandler(
            _ => JsonResponse(HttpStatusCode.OK, "not-json")));

        var exception = await Assert.ThrowsAsync<XtreamClientException>(
            () => client.GetAccountInfoAsync());

        Assert.Equal(XtreamClientError.InvalidResponse, exception.Error);
    }

    [Fact]
    public async Task HttpError_ThrowsHttpErrorWithoutResponseBody()
    {
        var client = CreateClient(new StubHttpMessageHandler(
            _ => JsonResponse(
                HttpStatusCode.BadGateway,
                "provider detail containing test-password")));

        var exception = await Assert.ThrowsAsync<XtreamClientException>(
            () => client.GetAccountInfoAsync());

        Assert.Equal(XtreamClientError.HttpError, exception.Error);
        Assert.Equal(HttpStatusCode.BadGateway, exception.ProviderStatusCode);
        Assert.DoesNotContain("test-password", exception.Message);
    }

    [Fact]
    public async Task MissingOptionalFields_AreAccepted()
    {
        const string json = """
            { "user_info": { "auth": true, "status": "Active" } }
            """;
        var client = CreateClient(new StubHttpMessageHandler(
            _ => JsonResponse(HttpStatusCode.OK, json)));
        var service = new XtreamConnectionService(client);

        var result = await service.GetStatusAsync();

        Assert.True(result.IsUsable);
        Assert.Null(result.ExpirationDate);
        Assert.Null(result.ActiveConnections);
        Assert.Null(result.MaxConnections);
        Assert.Empty(result.AllowedOutputFormats);
        Assert.Null(result.Server);
    }

    [Fact]
    public async Task NumericStrings_AreParsedDefensively()
    {
        const string json = """
            {
              "user_info": {
                "auth": "1",
                "status": "Active",
                "is_trial": "1",
                "active_cons": "3",
                "max_connections": "5"
              },
              "server_info": {
                "port": 8080,
                "timestamp_now": "1893456000"
              }
            }
            """;
        var client = CreateClient(new StubHttpMessageHandler(
            _ => JsonResponse(HttpStatusCode.OK, json)));

        var account = await client.GetAccountInfoAsync();

        Assert.True(account.UserInfo?.Authenticated);
        Assert.True(account.UserInfo?.IsTrial);
        Assert.Equal(3, account.UserInfo?.ActiveConnections);
        Assert.Equal(5, account.UserInfo?.MaxConnections);
        Assert.Equal("8080", account.ServerInfo?.Port);
        Assert.Equal(1893456000, account.ServerInfo?.CurrentTimestamp);
    }

    [Fact]
    public async Task VodCategories_AreParsedAndUseExpectedAction()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK,
            """[{"category_id":12,"category_name":"Movies"}]"""));
        var categories = await CreateClient(handler).GetVodCategoriesAsync();

        var category = Assert.Single(categories);
        Assert.Equal("12", category.CategoryId);
        Assert.Equal("Movies", category.CategoryName);
        Assert.Contains("action=get_vod_categories", handler.LastRequestUri?.Query);
    }

    [Fact]
    public async Task VodStreams_AcceptNumericStringsAndMissingOptionalFields()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK,
            """[{"stream_id":"54672","name":"Example","category_id":7,"added":"1700000000","container_extension":"mp4"},{"stream_id":2}]"""));
        var movies = await CreateClient(handler).GetVodStreamsAsync();

        Assert.Equal(2, movies.Count);
        Assert.Equal(54672, movies[0].StreamId);
        Assert.Equal("7", movies[0].CategoryId);
        Assert.Equal(1700000000, movies[0].Added);
        Assert.Equal("mp4", movies[0].ContainerExtension);
        Assert.Null(movies[1].Name);
        Assert.Contains("action=get_vod_streams", handler.LastRequestUri?.Query);
    }

    [Fact]
    public async Task SeriesCategories_AreParsedAndUseExpectedAction()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK,
            """[{"category_id":"4","category_name":"Drama"}]"""));
        var categories = await CreateClient(handler).GetSeriesCategoriesAsync();

        Assert.Equal("Drama", Assert.Single(categories).CategoryName);
        Assert.Contains("action=get_series_categories", handler.LastRequestUri?.Query);
    }

    [Fact]
    public async Task Series_AcceptNumericStringsAndMissingOptionalFields()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK,
            """[{"series_id":"91","name":"A Series","category_id":4,"rating":8,"last_modified":"1700000001"},{"series_id":92}]"""));
        var series = await CreateClient(handler).GetSeriesAsync();

        Assert.Equal(2, series.Count);
        Assert.Equal(91, series[0].SeriesId);
        Assert.Equal("4", series[0].CategoryId);
        Assert.Equal("8", series[0].Rating);
        Assert.Equal(1700000001, series[0].LastModified);
        Assert.Null(series[1].Plot);
        Assert.Contains("action=get_series", handler.LastRequestUri?.Query);
    }

    [Fact]
    public async Task CatalogInvalidJson_ThrowsInvalidResponse()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, "{")));
        var exception = await Assert.ThrowsAsync<XtreamClientException>(() => client.GetVodStreamsAsync());
        Assert.Equal(XtreamClientError.InvalidResponse, exception.Error);
    }

    [Fact]
    public async Task CatalogHttpError_ThrowsSanitizedHttpError()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => JsonResponse(HttpStatusCode.BadGateway, "test-password")));
        var exception = await Assert.ThrowsAsync<XtreamClientException>(() => client.GetSeriesAsync());
        Assert.Equal(XtreamClientError.HttpError, exception.Error);
        Assert.DoesNotContain("test-password", exception.Message);
    }

    [Fact]
    public async Task Playback302_ReturnsAbsoluteLocationWithoutFollowingRedirect()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Found);
        response.Headers.Location = new Uri("http://stream.invalid:25461/movie/u/p/54672.mp4?token=secret");
        var handler = new StubHttpMessageHandler(_ => response);

        var result = await CreateClient(handler).ResolveMoviePlaybackUrlAsync(54672, "mp4");

        Assert.Equal(response.Headers.Location, result);
        Assert.Equal(1, handler.RequestCount);
        Assert.Equal("IPTVSmartersPlayer", handler.LastUserAgent);
        Assert.EndsWith("/movie/test-user/test-password/54672.mp4", handler.LastRequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task PlaybackNon302_ThrowsUnexpectedResponse()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var exception = await Assert.ThrowsAsync<XtreamClientException>(() => client.ResolveMoviePlaybackUrlAsync(1, "mkv"));
        Assert.Equal(XtreamClientError.UnexpectedResponse, exception.Error);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, XtreamClientError.CredentialsRejected)]
    [InlineData(HttpStatusCode.Forbidden, XtreamClientError.CredentialsRejected)]
    [InlineData(HttpStatusCode.NotFound, XtreamClientError.NotFound)]
    public async Task PlaybackProviderErrors_AreDistinguished(
        HttpStatusCode statusCode,
        XtreamClientError expectedError)
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(statusCode)));
        var exception = await Assert.ThrowsAsync<XtreamClientException>(
            () => client.ResolveMoviePlaybackUrlAsync(1, "mkv"));
        Assert.Equal(expectedError, exception.Error);
    }

    [Fact]
    public async Task PlaybackWithoutLocation_ThrowsMissingLocation()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Found)));
        var exception = await Assert.ThrowsAsync<XtreamClientException>(() => client.ResolveMoviePlaybackUrlAsync(1, "mkv"));
        Assert.Equal(XtreamClientError.MissingLocation, exception.Error);
    }

    [Theory]
    [InlineData("/relative/movie.mkv")]
    [InlineData("ftp://stream.invalid/movie.mkv")]
    public async Task PlaybackWithInvalidLocation_ThrowsInvalidLocation(string location)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Found);
        response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
        var client = CreateClient(new StubHttpMessageHandler(_ => response));
        var exception = await Assert.ThrowsAsync<XtreamClientException>(() => client.ResolveMoviePlaybackUrlAsync(1, "mkv"));
        Assert.Equal(XtreamClientError.InvalidLocation, exception.Error);
    }

    [Fact]
    public async Task PlaybackTimeout_ThrowsTimeout()
    {
        var handler = new StubHttpMessageHandler(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Found);
        });
        var exception = await Assert.ThrowsAsync<XtreamClientException>(
            () => CreateClient(handler, timeoutSeconds: 1).ResolveMoviePlaybackUrlAsync(1, "mkv"));
        Assert.Equal(XtreamClientError.Timeout, exception.Error);
    }

    [Fact]
    public async Task PlaybackNetworkError_ThrowsUnavailable()
    {
        var handler = new StubHttpMessageHandler(
            (Func<CancellationToken, HttpResponseMessage>)(_ => throw new HttpRequestException("network detail")));
        var exception = await Assert.ThrowsAsync<XtreamClientException>(
            () => CreateClient(handler).ResolveMoviePlaybackUrlAsync(1, "mkv"));
        Assert.Equal(XtreamClientError.Unavailable, exception.Error);
    }

    private static XtreamClient CreateClient(
        HttpMessageHandler handler,
        string baseUrl = "https://provider.invalid",
        string username = "test-user",
        string password = "test-password",
        int timeoutSeconds = 30)
    {
        var options = Options.Create(new XtreamOptions
        {
            BaseUrl = baseUrl,
            Username = username,
            Password = password,
            TimeoutSeconds = timeoutSeconds
        });

        return new XtreamClient(new HttpClient(handler), options);
    }

    private static HttpResponseMessage JsonResponse(
        HttpStatusCode statusCode,
        string content) => new(statusCode)
    {
        Content = new StringContent(content)
    };

    private sealed class StubHttpMessageHandler(
        Func<CancellationToken, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        public StubHttpMessageHandler(
            Func<CancellationToken, HttpResponseMessage> responseFactory)
            : this(cancellationToken => Task.FromResult(responseFactory(cancellationToken)))
        {
        }

        public Uri? LastRequestUri { get; private set; }
        public int RequestCount { get; private set; }
        public string? LastUserAgent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequestUri = request.RequestUri;
            LastUserAgent = request.Headers.UserAgent.ToString();
            return responseFactory(cancellationToken);
        }
    }
}
