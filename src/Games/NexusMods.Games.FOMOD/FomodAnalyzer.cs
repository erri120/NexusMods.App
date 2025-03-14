﻿using System.Text;
using FomodInstaller.Scripting.XmlScript;
using JetBrains.Annotations;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Extensions.BCL;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.FOMOD;

public static class FomodAnalyzer
{
    public static async ValueTask<FomodAnalyzerInfo?> AnalyzeAsync(
        KeyedBox<RelativePath, LibraryArchiveTree> allFiles,
        IFileSystem fileSystem,
        IFileStore fileStore,
        CancellationToken cancellationToken = default)
    {
        if (!allFiles.GetFiles().TryGetFirst(x => x.Item.Path.EndsWith(FomodConstants.XmlConfigRelativePath), out var xmlNode))
            return null;

        // If the fomod folder is not at first level, find the prefix.
        var pathPrefix = xmlNode!.Parent()!.Parent()!;

        // Now get the actual items out.
        // Determine if this is a supported FOMOD.
        string? data;
        var images = new List<FomodAnalyzerInfo.FomodAnalyzerImage>();

        try
        {
            var hash = xmlNode!.Item.LibraryFile.Value.Hash;
            await using var stream = await fileStore.GetFileStream(hash, cancellationToken);
            using var streamReader = new StreamReader(stream, leaveOpen:true);
            data = await streamReader.ReadToEndAsync(cancellationToken);
            var xmlScript = new XmlScriptType();
            var script = (XmlScript)xmlScript.LoadScript(data, true);

            // Get all images listed in the FOMOD script.
            async Task AddImageIfValid(string? imagePathFragment)
            {
                if (string.IsNullOrEmpty(imagePathFragment))
                    return;

                var imagePath = pathPrefix.Item.Path.Join(RelativePath.FromUnsanitizedInput(imagePathFragment));
                byte[] bytes;
                try
                {
                    var node = allFiles.FindByPathFromChild(imagePath);
                    await using var imageStream = await fileStore.GetFileStream(node!.Item.LibraryFile.Value.Hash, cancellationToken);
                    using var ms = new MemoryStream();
                    await imageStream.CopyToAsync(ms, cancellationToken);
                    bytes = ms.ToArray();
                }
                catch (Exception)
                {
                    bytes = await GetPlaceholderImage(fileSystem, cancellationToken);
                }

                images.Add(new FomodAnalyzerInfo.FomodAnalyzerImage(imagePath, bytes));
            }

            await AddImageIfValid(script.HeaderInfo.ImagePath);
            foreach (var step in script.InstallSteps)
            foreach (var optionGroup in step.OptionGroups)
            foreach (var option in optionGroup.Options)
                await AddImageIfValid(option.ImagePath);
        }
        catch (Exception)
        {
            return null;
        }

        // TODO: We use Base64 here, which is really, really inefficient. We should zip up the images and store them separately in a non-SOLID archive.
        // Add all images to analysis output.
        return new FomodAnalyzerInfo
        {
            XmlScript = data,
            Images = images,
            PathPrefix = pathPrefix.Item.Path
        };
    }

    public static Task<byte[]> GetPlaceholderImage(IFileSystem fileSystem, CancellationToken ct = default)
    {
        return fileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("Assets/InvalidImagePlaceholder.png").ReadAllBytesAsync(ct);
    }
}

public record FomodAnalyzerInfo
{
    public required string XmlScript { get; init; }
    public required List<FomodAnalyzerImage> Images { get; init; }

    public required RelativePath PathPrefix { get; init; }

    public record struct FomodAnalyzerImage(string Path, byte[] Image);

    // Keeping in case this is ever needed. We can remove this once all FOMOD stuff is done.
    [PublicAPI]
    public async Task DumpToFileSystemAsync(AbsolutePath fomodFolder)
    {
        var fs = FileSystem.Shared;
        // Dump Item
        async Task DumpItem(string relativePath, byte[] data)
        {
            var finalPath = fomodFolder.Combine(relativePath);
            try
            {
                fs.CreateDirectory(finalPath.Parent);
                await fs.WriteAllBytesAsync(finalPath, data);
            }
            catch (IOException)
            {
                // ignored, this is a pathological case where path is broken 
            }

        }

        // Dump Xml
        await DumpItem(FomodConstants.XmlConfigRelativePath, Encoding.UTF8.GetBytes(XmlScript));

        // Dump Images
        foreach (var image in Images)
            await DumpItem(image.Path, image.Image);
    }
}
