using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.UI.Settings;
using NexusMods.Backend;
using NexusMods.Collections;
using NexusMods.CrossPlatform;
using NexusMods.DataModel;
using NexusMods.Games.AdvancedInstaller;
using NexusMods.Games.FileHashes;
using NexusMods.Games.FOMOD;
using NexusMods.Games.Generic;
using NexusMods.Games.TestHarness;
using NexusMods.Library;
using NexusMods.Networking.EpicGameStore;
using NexusMods.Networking.GitHub;
using NexusMods.Networking.GOG;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.Sdk.Settings;
using NexusMods.Sdk.Tracking;
using NexusMods.Telemetry;

namespace NexusMods.App.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Configure(builder.Services, trackingSettings: new TrackingSettings());
        builder.Services.AddRazorPages();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages().WithStaticAssets();

        app.Run();
    }

    private static IServiceCollection Configure(IServiceCollection services, TrackingSettings trackingSettings)
    {
        services.Configure<HostOptions>(options =>
        {
            // Sequential execution can lead to long startup times depending on number of hostedServices.
            options.ServicesStartConcurrently = true;
            // If executed sequentially, one service taking a long time can trigger the timeout,
            // preventing StopAsync of other services from being called. 
            options.ServicesStopConcurrently = true;
        });

        services = AddSupportedGames(services);
        services = AddHacks(services);

        return services
            .AddEpicGameStore()
            .AddSingleton<TimeProvider>(_ => TimeProvider.System)
            .AddDataModel()
            .AddLibrary()
            .AddLibraryModels()
            .AddJobMonitor()
            .AddNexusModsCollections()
            .AddSettings<TrackingSettings>()
            .AddSettings<LoggingSettings>()
            .AddSettings<ExperimentalSettings>()
            .AddTelemetry(trackingSettings)
            .AddTrackingOld(trackingSettings)
            .AddTracking(trackingSettings)
            .AddSettingsManager()
            .AddAdvancedInstaller()
            .AddFileExtractors()
            .AddSerializationAbstractions()
            .AddOSInterop()
            .AddRuntimeDependencies()
            .AddGames()
            .AddGameServices()
            .AddGenericGameSupport()
            .AddLoadoutAbstractions()
            .AddFomod()
            .AddNexusWebApi()
            .AddHttpDownloader()
            .AddTestHarness()
            .AddFileSystem()
            .AddGOG()
            .AddFileHashes()
            .AddGitHubApi();
    }

    private static IServiceCollection AddHacks(IServiceCollection services)
    {
        return services.AddSingleton<IGuidedInstaller>(new NullGuidedInstaller());
    }

    private static IServiceCollection AddSupportedGames(IServiceCollection services)
    {
        Games.RedEngine.Services.AddRedEngineGames(services);
        Games.StardewValley.Services.AddStardewValley(services);
        Games.Larian.BaldursGate3.Services.AddBaldursGate3(services);
        Games.CreationEngine.Services.AddCreationEngine(services);
        Games.MountAndBlade2Bannerlord.Services.AddMountAndBlade2Bannerlord(services);
        return services;
    }
}
