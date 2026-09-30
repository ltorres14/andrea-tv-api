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

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return responseFactory(cancellationToken);
        }
    }
}
