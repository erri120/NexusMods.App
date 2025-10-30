using JetBrains.Annotations;
using NexusMods.Sdk.Hashes;
using TransparentValueObjects;

namespace NexusMods.Sdk.Games;

[PublicAPI]
[ValueObject<uint>]
public readonly partial struct GameId
{
    private static readonly FNV1a32Pool HashPool = new(name: nameof(GameId));

    public static GameId From(string value) => From(HashPool.GetOrAdd(value));

    /// <inheritdoc/>
    public override string ToString() => HashPool[Value];
}
