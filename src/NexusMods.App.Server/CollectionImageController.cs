using Microsoft.AspNetCore.Mvc;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.Server;

[Route("collection-image")]
public class CollectionImageController : Controller
{
    private readonly IConnection _connection;
    private readonly HttpClient _client;

    public CollectionImageController(IConnection connection, HttpClient client)
    {
        _connection = connection;
        _client = client;
    }

    [HttpGet("{collectionId}")]
    public async ValueTask<IActionResult> Get(ulong collectionId, CancellationToken cancellationToken)
    {
        var entityId = EntityId.From(collectionId);
        var collection = CollectionMetadata.Load(_connection.Db, entityId);
        if (!collection.IsValid() || !collection.TileImageUri.HasValue) return NotFound();

        var uri = collection.TileImageUri.Value;
        var response = await _client.GetAsync(uri, cancellationToken: cancellationToken).ConfigureAwait(false);
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        return new FileStreamResult(stream, contentType: Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(mediaType ?? "image/webp"));
    }
}
