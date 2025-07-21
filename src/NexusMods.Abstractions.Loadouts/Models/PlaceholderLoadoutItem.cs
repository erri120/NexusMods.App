using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Represents an item with data that hasn't materialized yet.
/// </summary>
[PublicAPI]
public partial class PlaceholderLoadoutItem : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.PlaceholderLoadoutItem";

    public static readonly MarkerAttribute Placeholder = new(Namespace, nameof(Placeholder)) { IsIndexed = true };
}
