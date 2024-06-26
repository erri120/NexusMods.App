using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;

namespace NexusMods.Games.TestFramework.Verifiers;

[UsedImplicitly]
internal record VerifiableMod
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required ModCategory Category { get; init; }

    public required List<VerifiableFile> Files { get; init; }

    public static VerifiableMod From(Mod.ReadOnly mod)
    {
        var files = mod.Files
            .Where(f => f.TryGetAsStoredFile(out _))
            .Select(f =>
                {
                    f.TryGetAsStoredFile(out var stored);
                    return stored;
                }
            )
            .Select(VerifiableFile.From)
            .OrderByDescending(file => file.To, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(file => file.Hash)
            .ToList();

        return new VerifiableMod
        {
            Name = mod.Name,
            Version = mod.Version,
            Category = mod.Category,
            Files = files,
        };
    }
}

[UsedImplicitly]
internal record VerifiableFile
{
    public required string To { get; init; }

    public required ulong Size { get; init; }

    public required ulong Hash { get; init; }

    public static VerifiableFile From(StoredFile.ReadOnly storedFile)
    {
        return new VerifiableFile
        {
            To = storedFile.AsFile().To.ToString(),
            Size = storedFile.Size.Value,
            Hash = storedFile.Hash.Value,
        };
    }
}
