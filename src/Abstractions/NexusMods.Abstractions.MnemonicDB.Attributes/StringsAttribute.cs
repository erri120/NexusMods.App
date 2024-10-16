using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// A unordered collection of strings.
/// </summary>
public class StringsAttribute(string ns, string name) : CollectionAttribute<string, string>(ValueTag.Utf8, ns, name)
{
    protected override string ToLowLevel(string value) => value;

    protected override string FromLowLevel(string value, AttributeResolver resolver) => value;
}
