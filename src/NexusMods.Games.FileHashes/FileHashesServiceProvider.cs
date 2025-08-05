using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.FileHashes.DTOs;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.IO;

namespace NexusMods.Games.FileHashes;

internal class FileHashesServiceProvider : IFileHashesServiceProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly HttpClient _httpClient;
    private readonly IFileSystem _fileSystem;

    private readonly FileHashesServiceSettings _settings;
    private readonly AbsolutePath _storageDirectory;

    private readonly Dictionary<AbsolutePath, ConnectedDb> _databases = new();
    private readonly ScopedAsyncLock _lock = new();
    private ConnectedDb? _currentDb;

    private record ConnectedDb(IDb Db, DatomStore Store, Backend Backend, DatabaseInfo DatabaseInfo);
    private record struct DatabaseInfo(AbsolutePath Path, DateTimeOffset CreationTime);

    private AbsolutePath GameHashesReleaseFileName => _storageDirectory / _settings.ReleaseFilePath;

    public FileHashesServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _logger = serviceProvider.GetRequiredService<ILogger<FileHashesServiceProvider>>();
        _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        _jsonSerializerOptions = serviceProvider.GetRequiredService<JsonSerializerOptions>();
        _httpClient = serviceProvider.GetRequiredService<HttpClient>();
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

        var settingsManager = serviceProvider.GetRequiredService<ISettingsManager>();
        _settings = settingsManager.Get<FileHashesServiceSettings>();

        _storageDirectory = _settings.HashDatabaseLocation.ToPath(_fileSystem);
        _storageDirectory.CreateDirectory();

        Startup();
    }

    /// <inheritdoc/>
    public ValueTask<IFileHashesService> GetService(CancellationToken cancellationToken = default)
    {
        if (_currentDb is not null) return ValueTask.FromResult<IFileHashesService>(new FileHashesService(_currentDb.Db, _loggerFactory.CreateLogger<FileHashesService>()));
        return Inner();

        async ValueTask<IFileHashesService> Inner()
        {
            await CheckForUpdateCore(forceUpdate: false, cancellationToken: cancellationToken);
            if (_currentDb is null) throw new InvalidOperationException($"No database connected");
            return new FileHashesService(_currentDb.Db, _loggerFactory.CreateLogger<FileHashesService>());
        }
    }

    /// <inheritdoc/>
    public async ValueTask CheckForDbUpdate(bool forceUpdate = false, CancellationToken cancellationToken = default)
    {
        await CheckForUpdateCore(forceUpdate: forceUpdate, cancellationToken: cancellationToken);
    }

    private void Startup()
    {
        var existingDatabases = ExistingDBs().ToArray();

        // Cleanup old databases
        foreach (var databaseInfo in existingDatabases.Skip(1))
        {
            databaseInfo.Path.DeleteDirectory(true);
        }

        if (existingDatabases.TryGetFirst(out var latestDatabase))
        {
            _currentDb = OpenDb(latestDatabase);
        }

        Task.Run(async () => await CheckForUpdateCore(forceUpdate: false, cancellationToken: CancellationToken.None));
    }

    private ConnectedDb OpenDb(DatabaseInfo databaseInfo)
    {
        try
        {
            if (_databases.TryGetValue(databaseInfo.Path, out var existing))
                return existing;

            _logger.LogInformation("Opening hash database at {Path} for {Timestamp}", databaseInfo.Path, databaseInfo.CreationTime);
            var backend = new Backend(readOnly: true);
            var settings = new DatomStoreSettings
            {
                Path = databaseInfo.Path,
            };

            var store = new DatomStore(_loggerFactory.CreateLogger<DatomStore>(), settings, backend);
            var connection = new Connection(_loggerFactory.CreateLogger<Connection>(), store, _serviceProvider, [], readOnlyMode: true);
            var connectedDb = new ConnectedDb(connection.Db, store, backend, databaseInfo);

            _databases[databaseInfo.Path] = connectedDb;
            return connectedDb;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening database at {Path}", databaseInfo.Path);
            throw;
        }
    }

    private IEnumerable<DatabaseInfo> ExistingDBs()
    {
        return _storageDirectory
            .EnumerateDirectories(recursive: false)
            .Where(d => !d.FileName.EndsWith("_tmp"))
            .Select(path =>
            {
                // Format is "{guid}_{timestamp}"
                var parts = path.FileName.Split('_');
                if (parts.Length != 2 || !ulong.TryParse(parts[1], out var timestamp)) return Optional<DatabaseInfo>.None;
                return new DatabaseInfo(Path: path, CreationTime: DateTimeOffset.FromUnixTimeSeconds((long)timestamp));
            })
            .Where(static optional => optional.HasValue)
            .Select(static optional => optional.Value)
            .OrderByDescending(static databaseInfo => databaseInfo.CreationTime);
    }

    private bool ShouldCheckForUpdate()
    {
        if (!GameHashesReleaseFileName.FileExists) return true;
        var lastUpdated = GameHashesReleaseFileName.FileInfo.LastWriteTimeUtc;
        var diff = DateTime.UtcNow - lastUpdated;
        return diff >= _settings.HashDatabaseUpdateInterval;
    }

    private async Task CheckForUpdateCore(bool forceUpdate, CancellationToken cancellationToken = default)
    {
        using var _ = await _lock.LockAsync();

        var existingDatabases = ExistingDBs().ToArray();
        var shouldCheckForUpdate = forceUpdate || existingDatabases.Length == 0 || ShouldCheckForUpdate();

        if (!shouldCheckForUpdate && existingDatabases.TryGetFirst(out var latestDatabase))
        {
            _currentDb = OpenDb(latestDatabase);
            return;
        }

        Manifest? latestReleaseManifest = null;
        if (shouldCheckForUpdate)
        {
            latestReleaseManifest = await FetchLatestReleaseManifest(GameHashesReleaseFileName, cancellationToken: cancellationToken);
        }

        if (existingDatabases.Length == 0)
        {
            var embeddedDatabaseInfo = await AddEmbeddedDatabase(cancellationToken: cancellationToken);
            if (latestReleaseManifest is null)
            {
                if (!embeddedDatabaseInfo.HasValue)
                {
                    _logger.LogError("Failed to add embedded game hashes database and failed to fetch latest release manifest, game hashes functionality will be unavailable");
                    return;
                }

                _logger.LogWarning("Failed to fetch latest release manifest, defaulting to embedded game hashes database which may be out-of-date");
                _currentDb = OpenDb(embeddedDatabaseInfo.Value);
                return;
            }

            Debug.Assert(latestReleaseManifest is not null, "should've returned if we didn't have a manifest");
            if (embeddedDatabaseInfo.HasValue)
            {
                existingDatabases = ExistingDBs().ToArray();
                Debug.Assert(existingDatabases.Length == 1);
            }
        }

        if (latestReleaseManifest is null && existingDatabases.Length == 0)
        {
            _logger.LogError("Failed to fetch the latest release manifest and failed to use the embedded database, game hashes functionality will be unavailable");
            return;
        }

        if (latestReleaseManifest is null || existingDatabases[0].CreationTime >= latestReleaseManifest.CreatedAt)
        {
            _currentDb = OpenDb(existingDatabases[0]);
            return;
        }

        _logger.LogInformation("Fetching latest games hashes database");
        var releaseDatabaseInfo = await AddReleaseDatabase(latestReleaseManifest, cancellationToken);
        if (!releaseDatabaseInfo.HasValue) return;

        _currentDb = OpenDb(releaseDatabaseInfo.Value);
    }

    private async ValueTask<Manifest?> FetchLatestReleaseManifest(AbsolutePath storagePath, CancellationToken cancellationToken)
    {
        const int defaultTimeout = 15;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(delay: TimeSpan.FromSeconds(defaultTimeout));

        try
        {
            await using var fileStream = storagePath.Create();
            await using (var httpStream = await _httpClient.GetStreamAsync(_settings.GithubManifestUrl, cancellationToken: cts.Token))
            {
                await httpStream.CopyToAsync(fileStream, cancellationToken: cts.Token);
            }

            fileStream.Position = 0;
            return await JsonSerializer.DeserializeAsync<Manifest>(fileStream, _jsonSerializerOptions, cancellationToken: cts.Token);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to fetch latest release manifest from `{Url}`", _settings.GithubManifestUrl);
            return null;
        }
    }

    private async ValueTask<Optional<DatabaseInfo>> AddEmbeddedDatabase(CancellationToken cancellationToken)
    {
        try
        {
            var streamFactory = new EmbeddedResourceStreamFactory<FileHashesService>(resourceName: "games_hashes_db.zip");
            await using var archiveStream = await streamFactory.GetStreamAsync();
            var creationTime = ApplicationConstants.BuildDate;

            var path = await AddDatabase(archiveStream, creationTime, cancellationToken);
            return new DatabaseInfo(path, creationTime);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to add embedded database");
            return Optional<DatabaseInfo>.None;
        }
    }

    private async ValueTask<Optional<DatabaseInfo>> AddReleaseDatabase(Manifest releaseManifest, CancellationToken cancellationToken)
    {
        try
        {
            await using var httpStream = await _httpClient.GetStreamAsync(_settings.GameHashesDbUrl, cancellationToken: cancellationToken);
            var path = await AddDatabase(httpStream, releaseManifest.CreatedAt, cancellationToken: cancellationToken);
            return new DatabaseInfo(path, releaseManifest.CreatedAt);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to add release database from {Url}", _settings.GameHashesDbUrl);
            return Optional<DatabaseInfo>.None;
        }
    }

    private async ValueTask<AbsolutePath> AddDatabase(
        Stream archiveStream,
        DateTimeOffset databaseCreationTime,
        CancellationToken cancellationToken)
    {
        var name = $"{Guid.NewGuid()}_{databaseCreationTime.ToUnixTimeSeconds()}";

        await using var archivePath = new TemporaryPath(_fileSystem, _storageDirectory / $"{name}.zip");
        await using (var fileStream = archivePath.Path.Create())
        {
            await archiveStream.CopyToAsync(fileStream, cancellationToken: cancellationToken);
        }

        await using var extractionDirectory = new TemporaryPath(_fileSystem, _storageDirectory / $"{name}_tmp");
        await using (var fileStream = archivePath.Path.Read())
        using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
        {
            foreach (var fileEntry in zipArchive.Entries)
            {
                var destinationPath = extractionDirectory.Path.Combine(fileEntry.FullName);
                destinationPath.Parent.CreateDirectory();

                await using var entryStream = fileEntry.Open();
                await using var outputStream = destinationPath.Create();
                await entryStream.CopyToAsync(outputStream, cancellationToken: cancellationToken);
            }
        }

        var finalDirectory = _storageDirectory / name;
        Directory.Move(
            sourceDirName: extractionDirectory.Path.ToNativeSeparators(OSInformation.Shared),
            destDirName: finalDirectory.ToNativeSeparators(OSInformation.Shared)
        );

        return finalDirectory;
    }
}
