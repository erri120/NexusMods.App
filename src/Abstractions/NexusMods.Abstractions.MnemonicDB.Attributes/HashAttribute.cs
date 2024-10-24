using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Stores a <see cref="Hash"/> as a <see cref="ulong"/>.
/// </summary>
public class HashAttribute(string ns, string name) : ScalarAttribute<Hash, ulong>(ValueTag.UInt64, ns, name)
{
    /// <inheritdoc />
    protected override ulong ToLowLevel(Hash value)
    {
        return value.Value;
    }
    
    /// <inheritdoc />
    protected override Hash FromLowLevel(ulong value, AttributeResolver resolver)
    {
        return Hash.From(value);
    }
}
