using NexusMods.Sdk.Settings;

namespace NexusMods.UI.Sdk.Settings;

public static class BuilderExtensions
{
    /// <summary>
    /// Configures the settings type <typeparamref name="TSettings"/> to be
    /// exposed in the UI.
    /// </summary>
    public static ISettingsBuilder AddToUI<TSettings>(
        this ISettingsBuilder settingsBuilder,
        Func<ISettingsUIBuilder<TSettings>, ISettingsUIBuilder<TSettings>> configureUI
    ) where TSettings : class, ISettings, new()
    {
        settingsBuilder.AddConfiguration();
    }
}
