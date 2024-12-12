using GameFinder.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Paths;
using IGame = NexusMods.Abstractions.Games.IGame;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Base class for an individual service used to locate installed games.
/// </summary>
/// <typeparam name="TGameType">The underlying game type library which maps to the <see cref="GameFinder"/> library.</typeparam>
/// <typeparam name="TId">Unique identifier used by the store for the games.</typeparam>
/// <typeparam name="TGame">Implementation of <see cref="IGame"/> such as <see cref="ISteamGame"/> that allows us to retrieve info about the game.</typeparam>
/// <typeparam name="TParent"></typeparam>
public abstract class AGameLocator<TGameType, TId, TGame, TParent> : IGameLocator
    where TGame : ILocatableGame
    where TParent : AGameLocator<TGameType, TId, TGame, TParent>
    where TGameType : class, GameFinder.Common.IGame
    where TId : notnull
{
    private readonly ILogger _logger;

    private readonly AHandler<TGameType, TId> _handler;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="provider"></param>
    protected AGameLocator(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<TParent>>();
        _handler = provider.GetRequiredService<AHandler<TGameType, TId>>();
    }

    /// <summary>
    /// Acquires all found copies of a given game.
    /// </summary>
    /// <param name="game">
    ///     The game to find.
    ///     We use the unique store identifiers from this game to locate results.
    /// </param>
    /// <returns>List of found game installations.</returns>
    public IEnumerable<GameLocatorResult> Find(ILocatableGame game)
    {
        if (game is not TGame) yield break;

        var games = _handler.FindAllGamesById(out var errors);
        if (errors.Length != 0)
        {
            foreach (var error in errors) _logger.LogError("While looking for games: {Error}", error);
        }

        foreach (var kv in games)
        {
            var (id, value) = kv;
            _logger.LogInformation("Found game {Type}: {Value} ({Id})", value.GetType().Name, value, id);
            yield return new GameLocatorResult(Path(value), GetMappedFileSystem(value), Store, CreateMetadata(value));
        }
    }

    /// <inheritdoc/>
    public IEnumerable<GameLocatorResult> FindAll()
    {
        var games = _handler.FindAllGamesById(out var errors);
        if (errors.Length != 0)
        {
            foreach (var error in errors) _logger.LogError("While looking for games: {Error}", error);
        }

        foreach (var kv in games)
        {
            var (id, value) = kv;
            _logger.LogInformation("Found game {Type}: {Value} ({Id})", value.GetType().Name, value, id);
            yield return new GameLocatorResult(Path(value), GetMappedFileSystem(value), Store, CreateMetadata(value));
        }
    }

    /// <summary>
    /// The <see cref="GameStore"/> associated with this <see cref="IGameLocator"/>.
    /// </summary>
    protected abstract GameStore Store { get; }

    /// <summary>
    /// Returns all unique identifiers for this game.
    /// </summary>
    /// <param name="game">The game to get the unique identifiers for.</param>
    /// <returns>All unique identifiers.</returns>
    protected abstract IEnumerable<TId> Ids(TGame game);

    /// <summary>
    /// Gets the path to the game's main installation folder.
    /// </summary>
    /// <param name="record">Absolute path to the folder storing the game.</param>
    /// <returns>Absolute path to game folder.</returns>
    protected abstract AbsolutePath Path(TGameType record);

    protected virtual IFileSystem GetMappedFileSystem(TGameType game) => Path(game).FileSystem;

    /// <summary>
    /// Creates <see cref="IGameLocatorResultMetadata"/> for the specific result.
    /// </summary>
    /// <param name="game"></param>
    /// <returns></returns>
    protected abstract IGameLocatorResultMetadata CreateMetadata(TGameType game);
}
