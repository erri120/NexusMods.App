using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Sdk.Settings;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>
/// </summary>
[PublicAPI]
public static class ServiceExtensions
{
    /// <summary>
    /// Registers a new settings section.
    /// </summary>
    public static IServiceCollection AddSettingsSection(this IServiceCollection serviceCollection, SettingsSectionSetup setup)
    {
        return serviceCollection.AddSingleton(setup);
    }

    /// <summary>
    /// Registers an override for <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// This method only exists for tests. As an example, SQLite has an in-memory
    /// mode that should be used for tests. You can create a new settings type for
    /// SQLite that has a UseInMemory property. This property will be set to false
    /// by default when using the program normally. For tests, we want to override
    /// this property.
    ///
    /// <see cref="OverrideSettingsForTests{T}"/> does exactly that. It allows you
    /// to override the value for a settings type. Everytime the current value of
    /// <typeparamref name="T"/> is fetched, the lambda <paramref name="overrideMethod"/>
    /// will be invoked. This allows you to override any value and configure settings
    /// for tests.
    /// </remarks>
    public static IServiceCollection OverrideSettingsForTests<T>(
        this IServiceCollection serviceCollection,
        Func<T, T> overrideMethod)
        where T : class, ISettings, new()
    {
        return serviceCollection.AddSingleton(new SettingsOverrideInformation(typeof(T), Hack));

        object Hack(object obj) => overrideMethod((T)obj);
    }

    /// <summary>
    /// Use the JSON storage backend for this setting.
    /// </summary>
    public static void UseJson<T>(this ISettingsStorageBackendBuilder<T> builder)
        where T : class, ISettings, new()
    {
        // builder.UseStorageBackend(JsonStorageBackend.StaticId);
    }
}
