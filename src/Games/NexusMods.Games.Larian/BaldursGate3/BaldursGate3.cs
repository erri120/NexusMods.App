﻿using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.Generic.Installers;
using NexusMods.Games.Larian.BaldursGate3.Installers;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.Larian.BaldursGate3;

public class BaldursGate3 : AGame, ISteamGame, IGogGame
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOSInformation _osInformation;
    public override string Name => "Baldur's Gate 3";

    public IEnumerable<uint> SteamIds => [1086940u];
    public IEnumerable<long> GogIds => [1456460669];

    public static GameDomain GameDomain => GameDomain.From("baldursgate3");
    public override GameDomain Domain => GameDomain;

    public BaldursGate3(IServiceProvider provider) : base(provider)
    {
        _serviceProvider = provider;
        _osInformation = provider.GetRequiredService<IOSInformation>();
    }

    public override GamePath GetPrimaryFile(GameStore store)
    {
        if (_osInformation.IsOSX)
            return new GamePath(LocationId.Game, "Contents/MacOS/Baldur's Gate 3");
        return new GamePath(LocationId.Game, "bin/bg3.exe");
    }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        var result = new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
            { LocationId.From("Mods"), fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Larian Studios/Baldur's Gate 3/Mods") },
            { LocationId.From("PlayerProfiles"), fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Larian Studios/Baldur's Gate 3/PlayerProfiles/Public") },
            { LocationId.From("ScriptExtenderConfig"), fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Larian Studios/Baldur's Gate 3/ScriptExtender") },
        };
        return result;
    }

    /// <inheritdoc />
    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
    {
        return
        [
        ];
    }

    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        new BG3SEInstaller(_serviceProvider),
        new GenericPatternMatchInstaller(_serviceProvider)
        {
            InstallFolderTargets =
            [
                // Pak mods
                // Examples:
                // - <see href="https://www.nexusmods.com/baldursgate3/mods/366?tab=description">ImpUI (ImprovedUI) Patch7Ready</see>
                // - <see href="https://www.nexusmods.com/baldursgate3/mods/11373?tab=description">NPC Visual Overhaul (WIP) - NPC VO</see>
                new InstallFolderTarget
                {
                    DestinationGamePath = new GamePath(LocationId.From("Mods"), ""),
                    KnownValidFileExtensions = [new Extension(".pak")],
                    FileExtensionsToDiscard =
                    [
                        KnownExtensions.Txt, KnownExtensions.Md, KnownExtensions.Pdf, KnownExtensions.Png,
                        KnownExtensions.Json, new Extension(".lnk"),
                    ],
                },

                // bin and NativeMods mods
                // Examples:
                // - <see href="https://www.nexusmods.com/baldursgate3/mods/944">Native Mod Loader</see>
                // - <see href="https://www.nexusmods.com/baldursgate3/mods/668?tab=files">Achievement Enabler</see>
                new InstallFolderTarget
                {
                    DestinationGamePath = new GamePath(LocationId.Game, "bin"),
                    KnownSourceFolderNames = ["bin"],
                    Names = ["NativeMods"],
                },

                // loose files Data mods
                // Examples:
                // - <see href="https://www.nexusmods.com/baldursgate3/mods/555?tab=description">Fast XP</see>
                new InstallFolderTarget
                {
                    DestinationGamePath = new GamePath(LocationId.Game, "Data"),
                    KnownSourceFolderNames = ["Data"],
                    Names = ["Generated", "Public"],
                },
            ],
        },
    ];

    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new BaldursGate3Synchronizer(provider);
    }

    // TODO: We are using Icon for both Spine and GameWidget and GameImage is unused. We should use GameImage for the GameWidget, but need to update all the games to have better images.
    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<BaldursGate3>("NexusMods.Games.Larian.Resources.BaldursGate3.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<BaldursGate3>("NexusMods.Games.Larian.Resources.BaldursGate3.icon.png");
}