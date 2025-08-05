using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Games.FileHashes;

[PublicAPI]
public interface IFileHashesServiceProvider
{
    /// <summary>
    /// Returns a service instance with the latest file hashes database.
    /// </summary>
    ValueTask<IFileHashesService> GetService(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the file hashes database if needed or if forced to.
    /// </summary>
    ValueTask CheckForDbUpdate(bool forceUpdate = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the file hashes service, which provides a way to download and update the file hashes database
/// </summary>
[PublicAPI]
public interface IFileHashesService
{
    /// <summary>
    /// The current file hashes database.
    /// </summary>
    public IDb Current { get; }

    /// <summary>
    /// Gets all known vanity versions for a given game.
    /// </summary>
    public IEnumerable<VanityVersion> GetKnownVanityVersions(GameId gameId);

    /// <summary>
    /// Gets all game files associated with the provided locator IDs.
    /// </summary>
    public IEnumerable<GameFileRecord> GetGameFiles(LocatorIdsWithGameStore locatorIdsWithGameStore);

    /// <summary>
    /// Tries to get a vanity version based on the locator IDs.
    /// </summary>
    public bool TryGetVanityVersion(LocatorIdsWithGameStore locatorIdsWithGameStore, out VanityVersion version);

    /// <summary>
    /// Tries to get all locator IDs for the given store and vanity version.
    /// </summary>
    public bool TryGetLocatorIdsForVanityVersion(GameStore gameStore, VanityVersion version, out LocatorId[] locatorIds);

    /// <summary>
    /// Gets all locator IDs for a given store and <see cref="VersionDefinition"/>.
    /// </summary>
    public LocatorId[] GetLocatorIdsForVersionDefinition(GameStore gameStore, VersionDefinition.ReadOnly versionDefinition);

    /// <summary>
    /// Suggest version data for a given game installation and files.
    /// </summary>
    public Optional<VersionData> SuggestVersionData(GameInstallation gameInstallation, IEnumerable<(GamePath Path, Hash Hash)> files);
}

/// <summary>
/// Tuple of many <see cref="LocatorId"/> and <see cref="GameLocators.VanityVersion"/>.
/// </summary>
public record struct VersionData(LocatorId[] LocatorIds, VanityVersion VanityVersion);
