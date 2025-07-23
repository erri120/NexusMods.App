using System.Collections.Frozen;
using System.Diagnostics;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Diagnostics.Emitters;

[PublicAPI]
public record LoadoutStateForEmitters(
    Loadout.ReadOnly Loadout,
    FrozenDictionary<GamePath, SyncNode> SyncTree,
    GamePath[] GamePaths
)
{
    public IDb Db => Loadout.Db;

    public bool TryGetLoadoutItem(SyncNode syncNode, out LoadoutItem.ReadOnly loadoutItem)
    {
        loadoutItem = default(LoadoutItem.ReadOnly);
        if (!syncNode.HaveLoadout) return false;

        var syncNodePart = syncNode.Loadout;
        if (!syncNodePart.HaveEntityId) return false;

        var tmp = LoadoutItem.Load(Db, syncNodePart.EntityId);
        if (!tmp.IsValid()) return false;

        loadoutItem = tmp;
        return true;
    }

    public static LoadoutStateForEmitters Create(Loadout.ReadOnly loadout, IReadOnlyDictionary<GamePath, SyncNode> syncTree)
    {
        var frozenSyncTree = syncTree.ToFrozenDictionary();
        var gamePaths = syncTree.Select(static kv => kv.Key).OrderDescending().ToArray();
        return new LoadoutStateForEmitters(loadout, frozenSyncTree, gamePaths);
    }
}

/// <summary>
/// Interface for diagnostic emitters that run on the entire <see cref="Loadout"/>.
/// </summary>
/// <remarks>
/// This interface should be implemented if the emitter has to look at the relationship
/// between mods to create diagnostics.
/// </remarks>
[PublicAPI]
public interface ILoadoutDiagnosticEmitter : IDiagnosticEmitter
{
    /// <summary>
    /// Diagnoses a loadout and creates instances of <see cref="Diagnostic"/>.
    /// </summary>
    [Obsolete("To be replaced with the overload that takes in the sync tree")]
    IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, CancellationToken cancellationToken);

    /// <summary>
    /// Diagnoses a loadout and creates instances of <see cref="Diagnostic"/>.
    /// </summary>
    IAsyncEnumerable<Diagnostic> Diagnose(LoadoutStateForEmitters loadoutState, CancellationToken cancellationToken)
    {
        return Diagnose(loadoutState.Loadout, cancellationToken);
    }
}
