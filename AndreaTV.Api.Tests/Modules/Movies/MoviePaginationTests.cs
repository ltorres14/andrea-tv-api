using AndreaTV.Api.Infrastructure.Xtream;
using AndreaTV.Api.Infrastructure.Xtream.Models;
using AndreaTV.Api.Modules.Movies.Catalog;
using AndreaTV.Api.Modules.Movies.Controllers;
using AndreaTV.Api.Modules.Movies.Models;
using AndreaTV.Api.Modules.Movies.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AndreaTV.Api.Tests.Modules.Movies;

public sealed class MoviePaginationTests
{
    [Fact]
    public async Task Defaults_ArePageOneAndFiftyItems()
    {
        var service = new RecordingMovieService();
        var controller = new MoviesController(service);

        var action = await controller.GetMovies();

        var result = Assert.IsType<OkObjectResult>(action.Result);
        var page = Assert.IsType<MovieCatalogPage>(result.Value);
        Assert.Equal(1, service.RequestedPage);
        Assert.Equal(50, service.RequestedPageSize);
        Assert.Equal(1, page.Page);
        Assert.Equal(50, page.PageSize);
    }

    [Fact]
    public async Task FirstPage_ReturnsFirstFiftyMovies()
    {
        var result = await CreateService(105).GetMoviesAsync(1, 50);

        Assert.Equal(50, result.Items.Count);
        Assert.Equal(1, result.Items[0].StreamId);
        Assert.Equal(50, result.Items[^1].StreamId);
    }

    [Fact]
    public async Task SecondPage_ReturnsNextFiftyMovies()
    {
        var result = await CreateService(105).GetMoviesAsync(2, 50);

        Assert.Equal(50, result.Items.Count);
        Assert.Equal(51, result.Items[0].StreamId);
        Assert.Equal(100, result.Items[^1].StreamId);
    }

    [Fact]
    public async Task LastPage_CanBePartial()
    {
        var result = await CreateService(105).GetMoviesAsync(3, 50);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(101, result.Items[0].StreamId);
        Assert.Equal(105, result.Items[^1].StreamId);
    }

    [Fact]
    public async Task InvalidPage_ReturnsBadRequestWithoutCallingService()
    {
        var service = new RecordingMovieService();
        var action = await new MoviesController(service).GetMovies(page: 0);

        Assert.IsType<BadRequestObjectResult>(action.Result);
        Assert.False(service.WasCalled);
    }

    [Fact]
    public async Task InvalidPageSize_ReturnsBadRequestWithoutCallingService()
    {
        var service = new RecordingMovieService();
        var action = await new MoviesController(service).GetMovies(pageSize: 0);

        Assert.IsType<BadRequestObjectResult>(action.Result);
        Assert.False(service.WasCalled);
    }

    [Fact]
    public async Task PageSizeAboveMaximum_ReturnsBadRequestWithoutCallingService()
    {
        var service = new RecordingMovieService();
        var action = await new MoviesController(service).GetMovies(pageSize: 101);

        Assert.IsType<BadRequestObjectResult>(action.Result);
        Assert.False(service.WasCalled);
    }

    [Fact]
    public async Task Totals_AreCalculatedFromCompleteCatalog()
    {
        var result = await CreateService(6267).GetMoviesAsync(1, 50);

        Assert.Equal(6267, result.TotalItems);
        Assert.Equal(126, result.TotalPages);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
    }

    private static MovieService CreateService(int count)
    {
        var movies = Enumerable.Range(1, count)
            .Select(id => new XtreamVodStream
            {
                StreamId = id,
                Name = $"Movie {id}",
                CategoryId = "56",
                ContainerExtension = "mkv"
            })
            .ToArray();
        return new MovieService(
            new StubMovieCatalogCache(movies),
            new StubXtreamClient(movies));
    }

    private sealed class RecordingMovieService : IMovieService
    {
        public bool WasCalled { get; private set; }
        public int RequestedPage { get; private set; }
        public int RequestedPageSize { get; private set; }

        public MovieStatus GetStatus() => new("ready");

        public Task<IReadOnlyList<MovieCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MovieCategory>>([]);

        public Task<MovieCatalogPage> GetMoviesAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            RequestedPage = page;
            RequestedPageSize = pageSize;
            return Task.FromResult(new MovieCatalogPage(page, pageSize, 0, 0, []));
        }

        public Task<MoviePlayback> ResolvePlaybackAsync(
            int streamId,
            string containerExtension,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubMovieCatalogCache(IReadOnlyList<XtreamVodStream> movies) : IMovieCatalogCache
    {
        public Task<MovieCatalogSnapshot> GetAsync(CancellationToken cancellationToken = default)
        {
            var items = movies.Select(movie => new MovieCatalogItem(
                movie.StreamId,
                movie.Name,
                movie.CategoryId,
                movie.StreamIcon,
                movie.Rating,
                movie.Added,
                movie.ContainerExtension)).ToArray();
            return Task.FromResult(new MovieCatalogSnapshot(
                MovieCatalogSnapshot.CurrentVersion,
                DateTimeOffset.UtcNow,
                [],
                items));
        }
    }

    private sealed class StubXtreamClient(IReadOnlyList<XtreamVodStream> movies) : IXtreamClient
    {
        public Task<IReadOnlyList<XtreamVodStream>> GetVodStreamsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(movies);

        public Task<XtreamAccountInfo> GetAccountInfoAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<XtreamCategory>> GetVodCategoriesAsync(CancellationToken cancellationToken = default) =>
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
}
