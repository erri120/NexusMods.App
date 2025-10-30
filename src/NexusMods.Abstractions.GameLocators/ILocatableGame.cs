namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Base interface for a game which can be located by a <see cref="IGameLocator"/>.
/// </summary>
public interface ILocatableGame
{
    /// <summary>
    /// Human readable name of the game.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Unique identifier of the game on Nexus Mods.
    /// </summary>
    Sdk.NexusModsApi.GameId NexusModsGameId { get; }

    /// <summary>
    /// Unique identifier of the game.
    /// </summary>
    Sdk.Games.GameId GameId { get; }
}
