using AndreaTV.Api.Configuration;
using AndreaTV.Api.Infrastructure.Xtream;
using AndreaTV.Api.Infrastructure.Xtream.Models;
using AndreaTV.Api.Modules.Movies.Catalog;
using AndreaTV.Api.Modules.Movies.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AndreaTV.Api.Tests.Modules.Movies;

public sealed class MovieCatalogCacheTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task MissingSnapshot_DownloadsAndPersistsCatalog()
    {
        var repository = new MemoryRepository();
        var client = new CatalogXtreamClient();
        using var provider = CreateProvider(client);
        using var cache = CreateCache(repository, provider, Now);

        var result = await cache.GetAsync();

        Assert.Single(result.Categories);
        Assert.Single(result.Movies);
        Assert.Equal(1, client.CategoryCalls);
        Assert.Equal(1, client.MovieCalls);
        Assert.Equal(result, repository.Snapshot);
    }

    [Fact]
    public async Task FreshSnapshot_IsReusedWithoutCallingProvider()
    {
        var snapshot = Snapshot(Now.AddHours(-2));
        var repository = new MemoryRepository(snapshot);
        var client = new CatalogXtreamClient { FailCatalogRequests = true };
        using var provider = CreateProvider(client);
        using var cache = CreateCache(repository, provider, Now);

        var first = await cache.GetAsync();
        var second = await cache.GetAsync();

        Assert.Same(first, second);
        Assert.Equal(0, client.CategoryCalls);
        Assert.Equal(0, client.MovieCalls);
    }

    [Fact]
    public async Task ExpiredSnapshot_IsRefreshedAfterTwentyFourHours()
    {
        var oldSnapshot = Snapshot(Now.AddHours(-24));
        var repository = new MemoryRepository(oldSnapshot);
        var client = new CatalogXtreamClient();
        using var provider = CreateProvider(client);
        using var cache = CreateCache(repository, provider, Now);

        var result = await cache.GetAsync();

        Assert.Equal(Now, result.RefreshedAtUtc);
        Assert.NotSame(oldSnapshot, result);
        Assert.Equal(1, client.CategoryCalls);
        Assert.Equal(1, client.MovieCalls);
        Assert.Same(result, repository.Snapshot);
    }

    [Fact]
    public async Task FailedRefresh_ReturnsLastValidSnapshot()
    {
        var oldSnapshot = Snapshot(Now.AddDays(-2));
        var repository = new MemoryRepository(oldSnapshot);
        var client = new CatalogXtreamClient { FailCatalogRequests = true };
        using var provider = CreateProvider(client);
        using var cache = CreateCache(repository, provider, Now);

        var result = await cache.GetAsync();
        var secondResult = await cache.GetAsync();

        Assert.Same(oldSnapshot, result);
        Assert.Same(oldSnapshot, secondResult);
        Assert.Same(oldSnapshot, repository.Snapshot);
        Assert.Equal(1, client.CategoryCalls);
        Assert.Equal(0, client.MovieCalls);
    }

    [Fact]
    public async Task PersistedSnapshot_SurvivesNewRepositoryAndCacheInstances()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"andreatv-catalog-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "catalog.json");
        try
        {
            var options = Options.Create(new MovieCatalogOptions { StoragePath = path, RefreshHours = 24 });
            var environment = new TestHostEnvironment(Path.GetTempPath());
            var originalRepository = new FileMovieCatalogRepository(options, environment);
            var snapshot = Snapshot(Now.AddHours(-1));
            await originalRepository.SaveAsync(snapshot);

            var restartedRepository = new FileMovieCatalogRepository(options, environment);
            var client = new CatalogXtreamClient { FailCatalogRequests = true };
            using var provider = CreateProvider(client);
            using var cache = CreateCache(restartedRepository, provider, Now);

            var result = await cache.GetAsync();

            Assert.Equal(snapshot.Version, result.Version);
            Assert.Equal(snapshot.RefreshedAtUtc, result.RefreshedAtUtc);
            Assert.Equal(snapshot.Categories, result.Categories);
            Assert.Equal(snapshot.Movies, result.Movies);
            Assert.Equal(0, client.CategoryCalls);
            Assert.Equal(0, client.MovieCalls);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static ServiceProvider CreateProvider(IXtreamClient client)
    {
        var services = new ServiceCollection();
        services.AddSingleton(client);
        return services.BuildServiceProvider();
    }

    private static MovieCatalogCache CreateCache(
        IMovieCatalogRepository repository,
        IServiceProvider provider,
        DateTimeOffset now) => new(
            repository,
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new MovieCatalogOptions { RefreshHours = 24 }),
            new FixedTimeProvider(now),
            NullLogger<MovieCatalogCache>.Instance);

    private static MovieCatalogSnapshot Snapshot(DateTimeOffset refreshedAt) => new(
        MovieCatalogSnapshot.CurrentVersion,
        refreshedAt,
        [new MovieCategory("56", "Movies")],
        [new MovieCatalogItem(1, "Movie", "56", null, null, null, "mkv")]);

    private sealed class MemoryRepository(MovieCatalogSnapshot? snapshot = null) : IMovieCatalogRepository
    {
        public MovieCatalogSnapshot? Snapshot { get; private set; } = snapshot;

        public Task<MovieCatalogSnapshot?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Snapshot);

        public Task SaveAsync(MovieCatalogSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }

    private sealed class CatalogXtreamClient : IXtreamClient
    {
        public bool FailCatalogRequests { get; init; }
        public int CategoryCalls { get; private set; }
        public int MovieCalls { get; private set; }

        public Task<IReadOnlyList<XtreamCategory>> GetVodCategoriesAsync(CancellationToken cancellationToken = default)
        {
            CategoryCalls++;
            if (FailCatalogRequests)
            {
                throw new XtreamClientException(XtreamClientError.Unavailable, "Provider unavailable.");
            }

            return Task.FromResult<IReadOnlyList<XtreamCategory>>(
                [new XtreamCategory { CategoryId = "56", CategoryName = "Movies" }]);
        }

        public Task<IReadOnlyList<XtreamVodStream>> GetVodStreamsAsync(CancellationToken cancellationToken = default)
        {
            MovieCalls++;
            if (FailCatalogRequests)
            {
                throw new XtreamClientException(XtreamClientError.Unavailable, "Provider unavailable.");
            }

            return Task.FromResult<IReadOnlyList<XtreamVodStream>>(
                [new XtreamVodStream { StreamId = 2, Name = "Fresh movie", ContainerExtension = "mp4" }]);
        }

        public Task<XtreamAccountInfo> GetAccountInfoAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<XtreamCategory>> GetSeriesCategoriesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<XtreamSeries>> GetSeriesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Uri> ResolveMoviePlaybackUrlAsync(
            int streamId,
            string containerExtension,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "AndreaTV.Api.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
