using System.Collections.Frozen;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;

namespace NexusMods.Abstractions.Diagnostics.Emitters;

[PublicAPI]
public record LoadoutStateForEmitters(Loadout.ReadOnly Loadout, FrozenDictionary<GamePath, SyncNode> SyncTree);

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
