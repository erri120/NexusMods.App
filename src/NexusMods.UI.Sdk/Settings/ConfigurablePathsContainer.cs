using JetBrains.Annotations;
using NexusMods.Sdk;
using NexusMods.Sdk.Settings;

namespace NexusMods.UI.Sdk.Settings;

/// <summary>
/// A settings container for a <see cref="ConfigurablePath"/> value.
/// </summary>
[PublicAPI]
public class ConfigurablePathsContainer : APropertyValueContainer<ConfigurablePath[]>
{
    public ConfigurablePathsContainer(
            ConfigurablePath[] value,
            ConfigurablePath[] defaultValue,
            Action<ISettingsManager, ConfigurablePath[]> updaterFunc,
            IEqualityComparer<ConfigurablePath[]>? equalityComparer = null)
        : base(value, defaultValue, updaterFunc, equalityComparer: equalityComparer) { }
}
