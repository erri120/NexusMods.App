using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Sdk.Settings;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>
/// </summary>
[PublicAPI]
public static class ServiceExtensions
{
    /// <summary>
    /// Registers a settings type.
    /// </summary>
    public static IServiceCollection AddSettings<T>(this IServiceCollection serviceCollection)
        where T : class, ISettings, new()
    {
        return serviceCollection.AddSingleton(new SettingsTypeInformation(
            ObjectType: typeof(T),
            DefaultValue: new T(),
            ConfigureLambda: T.Configure
        ));
    }

    /// <summary>
    /// Registers a settings storage backend in DI.
    /// </summary>
    /// <param name="serviceCollection">The Service Collection.</param>
    /// <param name="isDefault">
    /// Whether the backend should be registered as the default backend.
    /// </param>
    public static IServiceCollection AddSettingsStorageBackend<T>(
        this IServiceCollection serviceCollection,
        bool isDefault = false)
        where T : class, IBaseSettingsStorageBackend
    {
        serviceCollection = serviceCollection.AddSingleton<IBaseSettingsStorageBackend, T>();
        if (!isDefault) return serviceCollection;
        return serviceCollection
            .AddSingleton<T>()
            .AddSingleton<DefaultSettingsStorageBackend>(serviceProvider => new DefaultSettingsStorageBackend(serviceProvider.GetRequiredService<T>())
            );
    }
}
