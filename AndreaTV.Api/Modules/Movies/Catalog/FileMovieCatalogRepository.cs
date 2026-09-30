using System.Text.Json;
using AndreaTV.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AndreaTV.Api.Modules.Movies.Catalog;

public sealed class FileMovieCatalogRepository : IMovieCatalogRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string storagePath;

    public FileMovieCatalogRepository(
        IOptions<MovieCatalogOptions> options,
        IHostEnvironment environment)
    {
        var configuredPath = options.Value.StoragePath;
        storagePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(configuredPath, environment.ContentRootPath);
    }

    public async Task<MovieCatalogSnapshot?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(storagePath))
        {
            return null;
        }

        try
        {
            await using var stream = new FileStream(
                storagePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var snapshot = await JsonSerializer.DeserializeAsync<MovieCatalogSnapshot>(
                stream,
                SerializerOptions,
                cancellationToken);

            return snapshot is { Version: MovieCatalogSnapshot.CurrentVersion }
                && snapshot.Categories is not null
                && snapshot.Movies is not null
                && snapshot.Categories.Count > 0
                && snapshot.Movies.Count > 0
                    ? snapshot
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SaveAsync(
        MovieCatalogSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(storagePath)
            ?? throw new InvalidOperationException("Movie catalog storage path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{storagePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, snapshot, SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, storagePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
