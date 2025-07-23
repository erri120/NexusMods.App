using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;

namespace NexusMods.Games.StardewValley;

internal static class Queries
{
    public static int GetSMAPIManifests(LoadoutStateForEmitters loadoutState, bool onlyEnabled)
    {
        var manifestFileGamePaths = loadoutState.GamePaths
            .Where(static gamePath => gamePath.InFolder(Constants.ModsGamePath) && 
                                      gamePath.Path.EndsWith(Constants.ManifestFile)
            )
            .ToArray();

        foreach (var manifestFileGamePath in manifestFileGamePaths)
        {
            var syncNode = loadoutState.SyncTree[manifestFileGamePath];
            if (!syncNode.HaveLoadout)
            {
                // TODO: what should we do here?
                continue;
            }

            if (!loadoutState.TryGetLoadoutItem(syncNode, out var manifestLoadoutItem)) continue;

            // TODO: always true
            var shouldInclude = !onlyEnabled || manifestLoadoutItem.IsEnabled();
            if (!shouldInclude) continue;

            var parent = manifestLoadoutItem.Parent;
        }

        return manifestFileGamePaths.Length;

        // var entityIds = db.Datoms(
        //     (ManifestFile, Null.Instance),
        //     (LoadoutItem.Loadout, loadoutId)
        // );
        //
        // return entityIds
        //     .Select(entityId => Load(db, entityId))
        //     .Where(loadoutItem => loadoutItem.IsValid() && (!onlyEnabled || onlyEnabled && loadoutItem.AsLoadoutFile().AsLoadoutItemWithTargetPath().AsLoadoutItem().IsEnabled()));
    }
}
