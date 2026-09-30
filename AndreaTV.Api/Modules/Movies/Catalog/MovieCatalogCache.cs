using AndreaTV.Api.Configuration;
using AndreaTV.Api.Infrastructure.Xtream;
using AndreaTV.Api.Modules.Movies.Models;
using Microsoft.Extensions.Options;

namespace AndreaTV.Api.Modules.Movies.Catalog;

public sealed class MovieCatalogCache(
    IMovieCatalogRepository repository,
    IServiceScopeFactory scopeFactory,
    IOptions<MovieCatalogOptions> options,
    TimeProvider timeProvider,
    ILogger<MovieCatalogCache> logger) : IMovieCatalogCache, IDisposable
{
    private readonly SemaphoreSlim refreshLock = new(1, 1);
    private readonly TimeSpan refreshInterval = TimeSpan.FromHours(
        options.Value.RefreshHours > 0 ? options.Value.RefreshHours : 24);
    private MovieCatalogSnapshot? currentSnapshot;
    private DateTimeOffset? lastRefreshAttemptUtc;

    public async Task<MovieCatalogSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = currentSnapshot;
        if (IsFresh(snapshot))
        {
            return snapshot!;
        }

        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            snapshot = currentSnapshot;
            if (snapshot is null)
            {
                try
                {
                    snapshot = await repository.LoadAsync(cancellationToken);
                    currentSnapshot = snapshot;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogWarning(
                        "Local movie catalog could not be read due to {ErrorType}; attempting a provider refresh.",
                        exception.GetType().Name);
                }
            }

            if (IsFresh(snapshot))
            {
                return snapshot!;
            }

            if (snapshot is not null && RefreshWasAttemptedRecently())
            {
                return snapshot;
            }

            try
            {
                lastRefreshAttemptUtc = timeProvider.GetUtcNow();
                var refreshed = await DownloadAsync(cancellationToken);
                await repository.SaveAsync(refreshed, cancellationToken);
                currentSnapshot = refreshed;
                return refreshed;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (XtreamClientException exception)
            {
                if (snapshot is null)
                {
                    throw;
                }

                logger.LogWarning(
                    "Movie catalog refresh failed with provider error {Error}; serving the last valid snapshot.",
                    exception.Error);
                return snapshot;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    "Movie catalog refresh or persistence failed with {ErrorType}; the previous snapshot remains unchanged.",
                    exception.GetType().Name);
                if (snapshot is not null)
                {
                    return snapshot;
                }

                throw new XtreamClientException(
                    XtreamClientError.Unavailable,
                    "The local movie catalog is unavailable.",
                    innerException: exception);
            }
        }
        finally
        {
            refreshLock.Release();
        }
    }

    public void Dispose() => refreshLock.Dispose();

    private bool IsFresh(MovieCatalogSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return false;
        }

        var age = timeProvider.GetUtcNow() - snapshot.RefreshedAtUtc;
        return age >= TimeSpan.Zero && age < refreshInterval;
    }

    private bool RefreshWasAttemptedRecently()
    {
        if (lastRefreshAttemptUtc is not { } attemptedAt)
        {
            return false;
        }

        var elapsed = timeProvider.GetUtcNow() - attemptedAt;
        return elapsed >= TimeSpan.Zero && elapsed < refreshInterval;
    }

    private async Task<MovieCatalogSnapshot> DownloadAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IXtreamClient>();
        var categoriesTask = client.GetVodCategoriesAsync(cancellationToken);
        var moviesTask = client.GetVodStreamsAsync(cancellationToken);
        await Task.WhenAll(categoriesTask, moviesTask);

        var categories = categoriesTask.Result
            .Select(category => new MovieCategory(category.CategoryId, category.CategoryName))
            .ToArray();
        var movies = moviesTask.Result
            .Select(movie => new MovieCatalogItem(
                movie.StreamId,
                movie.Name,
                movie.CategoryId,
                movie.StreamIcon,
                movie.Rating,
                movie.Added,
                movie.ContainerExtension))
            .ToArray();

        if (categories.Length == 0 || movies.Length == 0)
        {
            throw new XtreamClientException(
                XtreamClientError.InvalidResponse,
                "The provider returned an empty movie catalog.");
        }

        return new MovieCatalogSnapshot(
            MovieCatalogSnapshot.CurrentVersion,
            timeProvider.GetUtcNow(),
            categories,
            movies);
    }
}
