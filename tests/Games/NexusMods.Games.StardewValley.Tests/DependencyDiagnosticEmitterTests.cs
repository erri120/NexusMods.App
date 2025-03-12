using System.Diagnostics;
using System.Text;
using DynamicData.Kernel;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Resources;
using NexusMods.Abstractions.Resources.IO;
using NexusMods.Games.StardewValley.Emitters;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.Games.StardewValley.Tests;

[Trait("RequiresNetworking", "True")]
public class DependencyDiagnosticEmitterTests : ALoadoutDiagnosticEmitterTest<DependencyDiagnosticEmitterTests, StardewValley, DependencyDiagnosticEmitter>
{
    public DependencyDiagnosticEmitterTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddStardewValley()
            .AddUniversalGameLocator<StardewValley>(new Version("1.6.14"));
    }

    public record struct DiagnosticSyncNode
    {
        public Optional<LoadoutFileId> LoadoutFileId { get; init; }
        public required Hash Hash { get; init; }

        public bool IsGameFile => !LoadoutFileId.HasValue;

        public Optional<LoadoutItem.ReadOnly> GetLoadoutItem(IDb db)
        {
            if (!LoadoutFileId.HasValue) return Optional<LoadoutItem.ReadOnly>.None;

            var loadoutFile = LoadoutFile.Load(db, LoadoutFileId.Value);
            if (!loadoutFile.IsValid()) return Optional<LoadoutItem.ReadOnly>.None;

            var loadoutItem = loadoutFile.AsLoadoutItemWithTargetPath().AsLoadoutItem();
            Debug.Assert(loadoutItem.HasParent(), "Expected file to have a parent");

            // TODO: re-do this after reworking installer groups and stuff
            var parent = loadoutItem.Parent;
            LibraryLinkedLoadoutItem.ReadOnly linkedLoadoutItem;

            while (!parent.TryGetAsLibraryLinkedLoadoutItem(out linkedLoadoutItem))
            {
                if (!parent.AsLoadoutItem().HasParent()) return Optional<LoadoutItem.ReadOnly>.None;
                parent = parent.AsLoadoutItem().Parent;
            }

            if (!linkedLoadoutItem.IsValid()) return Optional<LoadoutItem.ReadOnly>.None;
            return linkedLoadoutItem.AsLoadoutItemGroup().AsLoadoutItem();
        }

        internal static DiagnosticSyncNode FromSyncNode(SyncNode input)
        {
            Debug.Assert(input.SourceItemType is LoadoutSourceItemType.Loadout or LoadoutSourceItemType.Game, "Expected item to have type Loadout or Game");

            var entityId = input.Loadout.EntityId;
            Debug.Assert(input.SourceItemType != LoadoutSourceItemType.Loadout || entityId.Value != 0, message: "Expected item of type Loadout to have an entity id");
            Debug.Assert(input.SourceItemType != LoadoutSourceItemType.Game || entityId.Value == 0, message: "Expected item of type Game to not have an entity id");

            var loadoutFileId = entityId.Value == 0 ? Optional<LoadoutFileId>.None : Optional<LoadoutFileId>.Create(entityId);
            return new DiagnosticSyncNode
            {
                LoadoutFileId = loadoutFileId,
                Hash = input.Loadout.Hash,
            };
        }
    }

    public record struct DiagnosticSyncData
    {
        private readonly Dictionary<GamePath, SyncNode> _syncTree;

        public DiagnosticSyncData(Dictionary<GamePath, SyncNode> syncTree)
        {
            _syncTree = syncTree;
        }

        public IEnumerable<KeyValuePair<GamePath, DiagnosticSyncNode>> GetMatches(GamePath startsWith, RelativePath fileName)
        {
            foreach (var kv in _syncTree)
            {
                var (key, value) = kv;

                if (value.SourceItemType is not LoadoutSourceItemType.Loadout and not LoadoutSourceItemType.Game) continue;
                if (!key.StartsWith(startsWith)) continue;
                if (!key.FileName.Equals(fileName)) continue;

                var diagnosticSyncNode = DiagnosticSyncNode.FromSyncNode(value);
                yield return new KeyValuePair<GamePath, DiagnosticSyncNode>(key, diagnosticSyncNode);
            }
        }
    }

    [Fact]
    public async Task Test_Foo()
    {
        var loadout = await CreateLoadout();

        // SMAPI 4.1.10 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(2400), FileId.From(119630));

        // Farm Type Manager 1.24.0 (https://www.nexusmods.com/stardewvalley/mods/3231?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(3231), FileId.From(117244));

        loadout = Loadout.Load(Connection.Db, loadout);

        var syncTree = await Synchronizer.BuildSyncTree(loadout);
        var data = new DiagnosticSyncData(syncTree);

        var modsDirectory = new GamePath(LocationId.Game, "Mods");
        var manifestFileName = new RelativePath("manifest.json");

        var matches = data.GetMatches(modsDirectory, manifestFileName).ToArray();

        var pipeline = new FileStoreStreamLoader(FileStore)
            .ThenDo(false, static (_, _, resource, _) =>
            {
                using var streamReader = new StreamReader(stream: resource.Data, encoding: Encoding.UTF8);
                var json = streamReader.ReadToEnd();

                var manifest = Interop.SMAPIJsonHelper.Deserialize<SMAPIManifest>(json);
                ArgumentNullException.ThrowIfNull(manifest);

                return ValueTask.FromResult(resource.WithData(manifest));
            })
            .StoreInMemory<Hash, SMAPIManifest>(
                selector: static mod => mod.Manifest.AsLoadoutFile().Hash,
                keyComparer: EqualityComparer<Hash>.Default,
                shouldDeleteKey: (tuple, _) => ValueTask.FromResult(!SMAPIModLoadoutItem.Load(connection.Db, tuple.Item2.Id).IsValid())
            );

        await Emitter.Foo(loadout, syncTree, CancellationToken.None);
    }

    [Fact]
    public async Task Test_MissingRequiredDependency()
    {
        var loadout = await CreateLoadout();

        // SMAPI 4.1.10 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(2400), FileId.From(119630));

        // Farm Type Manager 1.24.0 (https://www.nexusmods.com/stardewvalley/mods/3231?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(3231), FileId.From(117244));

        var diagnostic = await GetSingleDiagnostic(loadout);
        var missingRequiredDependencyMessageData = diagnostic.Should().BeOfType<Diagnostic<Diagnostics.MissingRequiredDependencyMessageData>>(because: "Content Patcher is required for Farm Type Manager").Which.MessageData;
        missingRequiredDependencyMessageData.MissingDependencyModId.Should().Be("Pathoschild.ContentPatcher", because: "Farm Type Manager requires Content Patcher");

        // Content Patcher 2.5.3 (https://www.nexusmods.com/stardewvalley/mods/1915?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(1915), FileId.From(124659));

        await ShouldHaveNoDiagnostics(loadout, because: "The required dependency Content Patcher has been installed");

        await VerifyDiagnostic(diagnostic);
    }

    [Fact]
    public async Task Test_DisabledRequiredDependencyMessageData()
    {
        var loadout = await CreateLoadout();

        // SMAPI 4.1.10 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(2400), FileId.From(119630));

        // Farm Type Manager 1.24.0 (https://www.nexusmods.com/stardewvalley/mods/3231?tab=files)
        var farmTypeManager = await InstallModFromNexusMods(loadout, ModId.From(3231), FileId.From(117244));

        // Content Patcher 2.5.3 (https://www.nexusmods.com/stardewvalley/mods/1915?tab=files)
        var contentPatcher = await InstallModFromNexusMods(loadout, ModId.From(1915), FileId.From(124659));

        await ShouldHaveNoDiagnostics(loadout, because: "All required dependencies are installed and enabled");

        await DisableItem(contentPatcher);

        var diagnostic = await GetSingleDiagnostic(loadout);
        var disabledRequiredDependencyMessageData = diagnostic.Should().BeOfType<Diagnostic<Diagnostics.DisabledRequiredDependencyMessageData>>(because: "Content Patcher is disabled and required by Farm Type Manager").Which.MessageData;

        var dependency = disabledRequiredDependencyMessageData.Dependency.ResolveData(ServiceProvider, Connection);
        dependency.AsLoadoutItem().ParentId.Should().Be(contentPatcher.LoadoutItemGroupId, because: "Content Patcher is the dependency");

        var dependent = disabledRequiredDependencyMessageData.SMAPIMod.ResolveData(ServiceProvider, Connection);
        dependent.AsLoadoutItem().ParentId.Should().Be(farmTypeManager.LoadoutItemGroupId, because: "Farm Type Manager is the dependent");
        
        await VerifyDiagnostic(diagnostic);
    }

    [Fact]
    public async Task Test_DisabledRequiredDependency_Collections()
    {
        var loadout = await CreateLoadout();
        
        var collectionA = (await CreateCollection(loadout, "Collection A")).AsLoadoutItemGroup().LoadoutItemGroupId;
        var collectionB = (await CreateCollection(loadout, "Collection B")).AsLoadoutItemGroup().LoadoutItemGroupId;
        
        // SMAPI 4.1.10 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(2400), FileId.From(119630), parent: collectionA);
        
        // Farm Type Manager 1.24.0 (https://www.nexusmods.com/stardewvalley/mods/3231?tab=files)
        var farmTypeManager = await InstallModFromNexusMods(loadout, ModId.From(3231), FileId.From(117244), parent: collectionA);
        
        // Content Patcher 2.5.3 (https://www.nexusmods.com/stardewvalley/mods/1915?tab=files)
        var contentPatcherCollectionB = await InstallModFromNexusMods(loadout, ModId.From(1915), FileId.From(124659), parent: collectionB);
        
        await ShouldHaveNoDiagnostics(loadout, because: "All required dependencies are installed and enabled");
        
        await DisableItem(collectionB);
        
        var diagnostic = await GetSingleDiagnostic(loadout);
        var disabledRequiredDependencyMessageData = diagnostic.Should().BeOfType<Diagnostic<Diagnostics.DisabledRequiredDependencyMessageData>>(because: "Content Patcher is inside disabled collection and required by Farm Type Manager").Which.MessageData;
        
        var dependency = disabledRequiredDependencyMessageData.Dependency.ResolveData(ServiceProvider, Connection);
        dependency.AsLoadoutItem().ParentId.Should().Be(contentPatcherCollectionB.LoadoutItemGroupId, because: "Content Patcher is the dependency");
        
        var dependent = disabledRequiredDependencyMessageData.SMAPIMod.ResolveData(ServiceProvider, Connection);
        dependent.AsLoadoutItem().ParentId.Should().Be(farmTypeManager.LoadoutItemGroupId, because: "Farm Type Manager is the dependent");
        
        // Add a copy in another collection
        // Content Patcher 2.5.3 (https://www.nexusmods.com/stardewvalley/mods/1915?tab=files)
        var contentPatcherCollectionA = await InstallModFromNexusMods(loadout, ModId.From(1915), FileId.From(124659), parent: collectionA);
        
        await ShouldHaveNoDiagnostics(loadout, because: "The dependency ContentPatcher in collectionA is enabled");
        
        await DisableItem(contentPatcherCollectionA);
        
        var diagnostic2 = await GetSingleDiagnostic(loadout);
        diagnostic2.Should().BeOfType<Diagnostic<Diagnostics.DisabledRequiredDependencyMessageData>>(because: "Both content patcher mods are disabled and required by Farm Type Manager");

        await VerifyDiagnostic(diagnostic);
    }

    [Fact]
    public async Task Test_RequiredDependencyIsOutdated()
    {
        var loadout = await CreateLoadout();

        // SMAPI 4.1.10 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        await InstallModFromNexusMods(loadout, ModId.From(2400), FileId.From(119630));

        // Farm Type Manager 1.24.0 (https://www.nexusmods.com/stardewvalley/mods/3231?tab=files)
        var farmTypeManager = await InstallModFromNexusMods(loadout, ModId.From(3231), FileId.From(117244));

        // Content Patcher 1.30.4 (https://www.nexusmods.com/stardewvalley/mods/1915?tab=files)
        var contentPatcher = await InstallModFromNexusMods(loadout, ModId.From(1915), FileId.From(78987));

        var diagnostic = await GetSingleDiagnostic(loadout);
        var requiredDependencyIsOutdatedMessageData = diagnostic.Should().BeOfType<Diagnostic<Diagnostics.RequiredDependencyIsOutdatedMessageData>>(because: "Content Patcher is outdated and required by Farm Type Manager").Which.MessageData;

        requiredDependencyIsOutdatedMessageData.CurrentVersion.Should().Be("1.30.4", because: "Content Patcher 1.30.4 was installed");
        requiredDependencyIsOutdatedMessageData.MinimumVersion.Should().Be("2.0.0", because: "Farm Type Manager 1.24.0 requires at least version 2.0.0 of Content Patcher");

        var dependency = requiredDependencyIsOutdatedMessageData.Dependency.ResolveData(ServiceProvider, Connection);
        dependency.AsLoadoutItem().ParentId.Should().Be(contentPatcher.LoadoutItemGroupId, because: "Content Patcher is the dependency");

        var dependent = requiredDependencyIsOutdatedMessageData.Dependent.ResolveData(ServiceProvider, Connection);
        dependent.AsLoadoutItem().ParentId.Should().Be(farmTypeManager.LoadoutItemGroupId, because: "Farm Type Manager is the dependent");

        // Test disabled cases
        await DisableItem(farmTypeManager);
        
        (await GetAllDiagnostics(loadout)).OfType<Diagnostic<Diagnostics.RequiredDependencyIsOutdatedMessageData>>()
            .Should().BeEmpty(because: "The dependant Farm Type Manager is disabled");
        
        await EnableItem(farmTypeManager);
        await DisableItem(contentPatcher);
        
        (await GetAllDiagnostics(loadout)).OfType<Diagnostic<Diagnostics.RequiredDependencyIsOutdatedMessageData>>()
            .Should().BeEmpty(because: "The outdated dependency Content Patcher is disabled");
        
        await VerifyDiagnostic(diagnostic);
    }
    
}
