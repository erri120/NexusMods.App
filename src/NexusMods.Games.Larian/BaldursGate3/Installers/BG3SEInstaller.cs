using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees.Traits;
using NexusMods.Sdk;

namespace NexusMods.Games.Larian.BaldursGate3.Installers;

/// <summary>
/// Installer for the Baldur's Gate 3 Script Extender
/// <see href="https://github.com/Norbyte/bg3se">BG3SE GitHub Repository</see>
/// <see href="https://www.nexusmods.com/baldursgate3/mods/2172">BG3SE Nexus Mods Page</see>
/// </summary>
public class BG3SEInstaller : ALibraryArchiveInstaller
{
    public BG3SEInstaller(IServiceProvider serviceProvider) :
        base(serviceProvider, serviceProvider.GetRequiredService<ILogger<BG3SEInstaller>>())
    {
    }
    
    private const string BG3SEFileName = "DWrite.dll";

    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var tree = libraryArchive.GetTree();
        var nodes = tree.FindSubPathsByKeyUpward([BG3SEFileName]);
        if (!nodes.TryGetFirst(out var dllNode)) return ValueTask.FromResult<InstallerResult>(new NotSupported(Reason: $"Found no file named `{BG3SEFileName}`"));

        var parent = dllNode.Parent() ?? tree;

        foreach (var fileNode in parent.EnumerateFilesBfs())
        {
            var relativePath = RelativePath.FromUnsanitizedInput("bin").Join(fileNode.Value.Item.Path.DropFirst(parent.Depth()));
            _ = new LoadoutFile.New(transaction, out var id)
            {
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, id)
                {
                    TargetPath = (loadout.Id, LocationId.Game, relativePath),
                    LoadoutItem = new LoadoutItem.New(transaction, id)
                    {
                        Name = relativePath.Name,
                        LoadoutId = loadout.Id,
                        ParentId = loadoutGroup.Id,
                    },
                },
                Hash = fileNode.Value.Item.LibraryFile.Value.Hash,
                Size = fileNode.Value.Item.LibraryFile.Value.Size,
            };
        }

        return ValueTask.FromResult<InstallerResult>(new Success());
    }
}
