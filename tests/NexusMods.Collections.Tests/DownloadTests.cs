using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Extensions.BCL;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using Xunit.Abstractions;

namespace NexusMods.Collections.Tests;

[Trait("RequiresNetworking", "True")]
public class DownloadTests(ITestOutputHelper testOutputHelper) : ACyberpunkIsolatedGameTest<DownloadTests>(testOutputHelper)
{
    [Fact]
    public async Task Test()
    {
        var modId = ModId.From(5534);
        var modPageMetadata = await NexusModsLibrary.GetOrAddModPage(modId, Cyberpunk2077Game.GameIdStatic, CancellationToken.None);

        var expected = Md5HashValue.Parse("fd36418e2e5c063974af97a11f0a048e");
        uint[] fileIds = [29487, 29497, 29507, 29510, 29518];

        var archiveHashes = new List<ValueTuple<FileId, Md5HashValue>>();
        var pairs = new List<ValueTuple<RelativePath, Md5HashValue, FileId>>();

        foreach (var fileId in fileIds.Select(static x => FileId.From(x)))
        {
            var fileMetadata = await NexusModsLibrary.GetOrAddFile(fileId, modPageMetadata, CancellationToken.None);

            await using var destination = TemporaryFileManager.CreateFile();
            var job = await NexusModsLibrary.CreateDownloadJob(destination, fileMetadata, CancellationToken.None);

            var libraryFile = await LibraryService.AddDownload(job);

            {
                await using var stream = destination.Path.Read();
                using var md5Hasher = MD5.Create();
                var bytes = await md5Hasher.ComputeHashAsync(stream);
                var md5 = Md5HashValue.From(bytes);

                // 25b2b2690d058fcb3731782e9d51aed9
                archiveHashes.Add((fileId, md5));
            }

            libraryFile.TryGetAsLibraryArchive(out var libraryArchive).Should().BeTrue();
            libraryArchive.IsValid().Should().BeTrue();

            foreach (var child in libraryArchive.Children)
            {
                var hash = child.AsLibraryFile().Hash;
                await using var stream = await FileStore.GetFileStream(hash, CancellationToken.None);

                using var md5Hasher = MD5.Create();
                var bytes = await md5Hasher.ComputeHashAsync(stream);
                var md5 = Md5HashValue.From(bytes);

                pairs.Add((child.Path, md5, fileId));

                if (md5.Equals(expected)) throw new Exception();
            }
        }

        throw new InvalidCastException();
    }

    [Fact]
    public async Task Test_Issue2888()
    {
        // https://github.com/Nexus-Mods/NexusMods.App/issues/2888
        var slug = CollectionSlug.From("ls4c1l");
        var revision = RevisionNumber.From(113);

        NexusModsCollectionLibraryFile.ReadOnly nexusModsCollectionLibraryFile;

        {
            await using var destination = TemporaryFileManager.CreateFile();
            var downloadJob = NexusModsLibrary.CreateCollectionDownloadJob(destination, slug, revision, CancellationToken.None);
            var libraryFile = await LibraryService.AddDownload(downloadJob);
            libraryFile.TryGetAsNexusModsCollectionLibraryFile(out nexusModsCollectionLibraryFile).Should().BeTrue();
        }

        var collectionRevision = await NexusModsLibrary.GetOrAddCollectionRevision(nexusModsCollectionLibraryFile, slug, revision, CancellationToken.None);

        // NOTE(erri120): Download with index 726 failed to install in the reported issue
        collectionRevision.Downloads.TryGetFirst(x => x.ArrayIndex == 726, out var download).Should().BeTrue();
        download.IsValid().Should().BeTrue();
        download.TryGetAsCollectionDownloadNexusMods(out var nexusModsDownload).Should().BeTrue();
        nexusModsDownload.IsValid().Should().BeTrue();

        // "path": "r6\\scripts\\Talk to Me\\TalkToMe.reds",
        // "md5": "fd36418e2e5c063974af97a11f0a048e"

        var loginManager = ServiceProvider.GetRequiredService<ILoginManager>();

        var isLoggedIn = await loginManager.GetIsUserLoggedInAsync();
        isLoggedIn.Should().BeTrue();

        var isPremium = loginManager.IsPremium;
        isPremium.Should().BeTrue();

        var downloader = ServiceProvider.GetRequiredService<CollectionDownloader>();
        await downloader.Download(nexusModsDownload, CancellationToken.None);

        var loadout = await CreateLoadout();
        var collectionGroup = await InstallCollectionJob.Create(ServiceProvider, loadout, nexusModsCollectionLibraryFile, collectionRevision, []);

        var monitor = ServiceProvider.GetRequiredService<IJobMonitor>();

        var job = await InstallCollectionDownloadJob.Create(
            serviceProvider: ServiceProvider,
            targetLoadout: loadout,
            download: download,
            cancellationToken: CancellationToken.None
        );

        var loadoutItemGroup = await monitor.Begin<InstallCollectionDownloadJob, LoadoutItemGroup.ReadOnly>(job);
    }
}
