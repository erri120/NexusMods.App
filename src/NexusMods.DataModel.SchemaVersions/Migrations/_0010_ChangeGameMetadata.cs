using System.Buffers;
using System.Collections.Frozen;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Sdk;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.DataModel.SchemaVersions.Migrations;

public class _0010_ChangeGameMetadata : IScanningMigration
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

    public ScanResultType Update(ref KeyPrefix prefix, ReadOnlySpan<byte> valueSpan, in IBufferWriter<byte> writer)
    {
        if (prefix.A == _nameAttributeId) return ScanResultType.Delete;
        if (prefix.A != _gameIdAttributeId) return ScanResultType.None;

        if (prefix.ValueTag is ValueTag.UInt32)
        {
            var nexusModsGameId = NexusModsGameId.From(UInt32Serializer.Read(valueSpan));
            if (!_idMappings.TryGetValue(nexusModsGameId, out var gameId))
            {
                _logger.LogWarning("Unable to migrate datom {E} due to missing mapping for Nexus Mods game id {NexusModsGameId}", prefix.E, nexusModsGameId);
                return ScanResultType.None;
            }

            UInt64Serializer.Write(gameId.Value, writer);
            return ScanResultType.Update;
        } else if (prefix.ValueTag is ValueTag.UInt64)
        {
            var value = UInt64Serializer.Read(valueSpan);
            var gameId = GameId.From(value);

            if (!_idMappings.TryGetFirst(kv => kv.Value == gameId, out var pair))
            {
                _logger.LogWarning("Unknown game id `{GameId}` while scanning for migration", gameId);
                return ScanResultType.None;
            }

            return ScanResultType.None;
        }
        else
        {
            _logger.LogWarning("Unable to migrate datom {E} due to unsupported value tag {ValueTag}", prefix.E, prefix.ValueTag);
            return ScanResultType.None;
        }
    }
}
