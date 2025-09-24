using Microsoft.Extensions.DependencyInjection;
using NexusMods.Sdk.Settings;

namespace NexusMods.Backend;

public static class ServiceExtensions
{
    public static IServiceCollection AddSettingsManager(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<ISettingsManager, SettingsManager>()
            .AddStorageBackend<JsonStorageBackend>()
            .AddStorageBackend<MnemonicDBStorageBackend>(isDefault: true)
            .AddSettingModel();
    }
}
