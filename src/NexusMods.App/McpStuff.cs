using System.ComponentModel;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Telemetry;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App;

[McpServerResourceType]
public static class McpStuff
{
    [McpServerResource(UriTemplate = "mcp://collection-revisions", MimeType = "application/json")]
    [Description("Get all collection revisions added to the app")]
    [UsedImplicitly]
    public static string AddedCollectionRevisions(IServiceProvider serviceProvider)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();
        var gameDomainCache = serviceProvider.GetRequiredService<IGameDomainToGameIdMappingCache>();

        var revisions = CollectionRevisionMetadata.All(connection.Db);
        
        var results = revisions.Select(x => new AddedCollectionResult(
            CollectionName: x.Collection.Name,
            CollectionUri: NexusModsUrlBuilder.GetCollectionUri(gameDomainCache[x.Collection.GameId], x.Collection.Slug, x.RevisionNumber, campaign: "mcp"),
            CollectionSlug: x.Collection.Slug,
            RevisionNumber: x.RevisionNumber
        )).ToArray();

        var json = JsonSerializer.Serialize(results);
        return json;
    }

    [UsedImplicitly]
    private record AddedCollectionResult(string CollectionName, Uri CollectionUri, CollectionSlug CollectionSlug, RevisionNumber RevisionNumber);
}
