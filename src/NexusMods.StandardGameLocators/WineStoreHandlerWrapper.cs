using System.Diagnostics.CodeAnalysis;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.Wine;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;
using OneOf;
using IGame = GameFinder.Common.IGame;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Helper class that uses all registered store handlers to find games inside wine prefixes.
/// </summary>
public class WineStoreHandlerWrapper
{
    /// <summary>
    /// Delegate for creating a <see cref="AHandler"/> given a <see cref="AWinePrefix"/>, an implementation
    /// of <see cref="IRegistry"/> and an implementation of <see cref="IFileSystem"/>. The registry and
    /// file system are overlays and created from the wine prefix.
    /// </summary>
    public delegate (AHandler, ToResult) CreateHandler(AWinePrefix winePrefix, IRegistry wineRegistry, IFileSystem wineFileSystem);

    public delegate GameLocatorResult ToResult(IGame foundGame);

    public delegate bool Matches(GameLocatorResult foundGame, ILocatableGame requestedGame);

    private readonly IFileSystem _fileSystem;
    private readonly CreateHandler[] _factories;
    private readonly Matches[] _matches;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WineStoreHandlerWrapper(IFileSystem fileSystem, CreateHandler[] factories, Matches[] matches)
    {
        _fileSystem = fileSystem;
        _factories = factories;
        _matches = matches;
    }

    /// <summary>
    /// Tries to match the requested game with one of the found games and returns
    /// a non-null <see cref="GameLocatorResult"/> on success, else <c>null</c>.
    /// </summary>
    /// <param name="foundGames"></param>
    /// <param name="requestedGame"></param>
    /// <returns></returns>
    public GameLocatorResult? FindMatchingGame(IEnumerable<IGame> foundGames, ILocatableGame requestedGame)
    {
        // foreach (var foundGame in foundGames)
        // {
        //     foreach (var matcher in _matchers)
        //     {
        //         var res = matcher(foundGame, requestedGame);
        //         if (res is not null) return res;
        //     }
        // }

        return null;
    }

    /// <summary>
    /// Finds all games inside the given prefix using the registered store handlers.
    /// </summary>
    /// <param name="winePrefix"></param>
    /// <returns></returns>
    public IEnumerable<OneOf<GameLocatorResult, ErrorMessage>> FindAllGamesInPrefix(AWinePrefix winePrefix)
    {
        IRegistry wineRegistry;
        try
        {
            wineRegistry = winePrefix.CreateRegistry(_fileSystem);
        }
        catch (Exception)
        {
            wineRegistry = new InMemoryRegistry();
        }

        var wineFileSystem = winePrefix.CreateOverlayFileSystem(_fileSystem);

        foreach (var factory in _factories)
        {
            var (handler, toResult) = factory(winePrefix, wineRegistry, wineFileSystem);

            foreach (var res in handler.FindAllInterfaceGames())
            {
                yield return res.MapT0(game => toResult(game));
            }
        }
    }
}
