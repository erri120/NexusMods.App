using System.Buffers;
using System.Collections.Frozen;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Sdk;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.DataModel.SchemaVersions.Migrations;

public class _0010_ChangeGameMetadata : ITransactionalMigration
{
    public static (MigrationId Id, string Name) IdAndName { get; } = MigrationId.ParseNameAndId(nameof(_0010_ChangeGameMetadata));

    private readonly ILogger _logger;
    private readonly FrozenDictionary<NexusModsGameId, GameId> _idMappings;

    public _0010_ChangeGameMetadata(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<_0010_ChangeGameMetadata>>();

        var enumerable = serviceProvider
            .GetServices<ILocatableGame>()
            .Where(game => game.NexusModsGameId.HasValue)
            .Select(game => new KeyValuePair<NexusModsGameId, GameId>(game.NexusModsGameId.Value, game.GameId));

        // NOTE(erri120): special stupid case for stubbed game...
        var stubbedGame = serviceProvider.GetServices<ILocatableGame>().FirstOrDefault(game => game.GameId == GameId.From("StubbedGame"));
        if (stubbedGame is not null)
        {
            enumerable = enumerable.Append(new KeyValuePair<NexusModsGameId, GameId>(NexusModsGameId.From(uint.MaxValue), stubbedGame.GameId));
        }

        _idMappings = enumerable.ToFrozenDictionary();
    }

    private Optional<AttributeId> _gameIdAttributeId = Optional<AttributeId>.None;
    private Optional<AttributeId> _nameAttributeId = Optional<AttributeId>.None;

    public Task Prepare(IDb db)
    {
        var gameIdSymbol = db.AttributeCache.AllAttributeIds.FirstOrDefault(symbol => symbol is { Namespace: "NexusMods.Loadouts.GameMetadata", Name: "GameId" });
        if (gameIdSymbol is not null)
        {
            _gameIdAttributeId = db.AttributeCache.GetAttributeId(gameIdSymbol);
        }

        var nameSymbol = db.AttributeCache.AllAttributeIds.FirstOrDefault(symbol => symbol is { Namespace: "NexusMods.Loadouts.GameMetadata", Name: "Name" });
        if (nameSymbol is not null)
        {
            _nameAttributeId = db.AttributeCache.GetAttributeId(nameSymbol);
        }

        return Task.CompletedTask;
    }

    public void Migrate(ITransaction tx, IDb db)
    {
        MigrateGameId(tx, db);
        MigrateName(tx, db);
    }

    private void MigrateName(ITransaction tx, IDb db)
    {
        if (!_nameAttributeId.HasValue)
        {
            _logger.LogInformation("Skipping name migration, found no matching attribute");
            return;
        }

        var datoms = db.Datoms(new AttributeSlice(_nameAttributeId.Value));
        if (datoms.Count == 0)
        {
            _logger.LogInformation("Skipping name migration, found no datoms to migrate");
            return;
        }

        foreach (var datom in datoms)
        {
            tx.Add(datom.Retract());
        }
    }

    private void MigrateGameId(ITransaction tx, IDb db)
    {
        if (!_gameIdAttributeId.HasValue)
        {
            _logger.LogInformation("Skipping game id migration, found no matching attribute");
            return;
        }

        var datoms = db.Datoms(new AttributeSlice(_gameIdAttributeId.Value));
        if (datoms.Count == 0)
        {
            _logger.LogInformation("Skipping game id migration, found no datoms to migrate");
            return;
        }

        var writer = new ArrayBufferWriter<byte>(initialCapacity: sizeof(ulong));

        foreach (var datom in datoms)
        {
            if (datom.Prefix.ValueTag is not ValueTag.UInt32)
            {
                _logger.LogWarning("Datom considered for migration has value tag {Actual} instead of {Expected}", datom.Prefix.ValueTag, ValueTag.UInt32);
                continue;
            }

            var nexusModsGameId = NexusModsGameId.From(UInt32Serializer.Read(datom.ValueSpan));
            if (!_idMappings.TryGetValue(nexusModsGameId, out var gameId))
            {
                _logger.LogWarning("Unable to migrate datom due to missing mapping for Nexus Mods game id {NexusModsGameId}", nexusModsGameId);
                tx.Add(datom.Retract());
                continue;
            }

            UInt64Serializer.Write(gameId.Value, writer);
            tx.Add(datom.E, datom.A, ValueTag.UInt64, writer.WrittenSpan);
            writer.Clear();
        }
    }
}
