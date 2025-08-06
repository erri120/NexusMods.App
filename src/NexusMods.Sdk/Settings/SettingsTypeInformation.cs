namespace NexusMods.Sdk.Settings;

public record SettingsTypeInformation(
    Type ObjectType,
    ISettings DefaultValue,
    Func<ISettingsBuilder, ISettingsBuilder> ConfigureLambda
);
