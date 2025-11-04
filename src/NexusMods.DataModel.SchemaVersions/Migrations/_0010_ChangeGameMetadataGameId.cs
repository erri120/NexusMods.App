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

public class _0010_ChangeGameMetadataGameId : ITransactionalMigration
{
    public static (MigrationId Id, string Name) IdAndName { get; } = MigrationId.ParseNameAndId(nameof(_0010_ChangeGameMetadataGameId));

    private readonly ILogger _logger;
    private readonly FrozenDictionary<NexusModsGameId, GameId> _idMappings;
    private Optional<AttributeId> _attributeId = Optional<AttributeId>.None;

    public _0010_ChangeGameMetadataGameId(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<_0010_ChangeGameMetadataGameId>>();

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

    public Task Prepare(IDb db)
    {
        var symbol = db.AttributeCache.AllAttributeIds.FirstOrDefault(symbol => symbol is { Namespace: "NexusMods.Loadouts.GameMetadata", Name: "GameId" });
        if (symbol is null) return Task.CompletedTask;

        _attributeId = db.AttributeCache.GetAttributeId(symbol);
        return Task.CompletedTask;
    }

    public void Migrate(ITransaction tx, IDb db)
    {
        if (!_attributeId.HasValue)
        {
            _logger.LogInformation("Skipping migration, found no matching attribute");
            return;
        }

        var datoms = db.Datoms(new AttributeSlice(_attributeId.Value));
        if (datoms.Count == 0)
        {
            _logger.LogInformation("Skipping migration, found no datoms to migrate");
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
