using JetBrains.Annotations;

namespace NexusMods.Sdk.Settings;

/// <summary>
/// Configuration builder for types that implement <see cref="ISettings"/>.
/// </summary>
[PublicAPI]
public interface ISettingsBuilder
{
    /// <summary>
    /// Configure the default value to use a factory instead of new().
    /// </summary>
    /// <remarks>
    /// The type still requires a default constructor, even if it's not called.
    /// </remarks>
    ISettingsBuilder ConfigureDefault<TSettings>(
        Func<IServiceProvider, TSettings> defaultValueFactory
    ) where TSettings : class, ISettings, new();

    /// <summary>
    /// Configures the storage backend for this setting.
    /// </summary>
    ISettingsBuilder ConfigureStorageBackend<TSettings>(
        Action<ISettingsStorageBackendBuilder<TSettings>> configureStorageBackend
    ) where TSettings : class, ISettings, new();

    ISettingsBuilder AddConfiguration();
}
