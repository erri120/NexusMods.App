using Microsoft.AspNetCore.Mvc.RazorPages;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.App.Server.Pages;

public class IndexModel : PageModel
{
    private readonly IConnection _connection;

    public Entities<CollectionRevisionMetadata.ReadOnly> CollectionRevisions { get; set; }

    public IndexModel(IConnection connection)
    {
        _connection = connection;
    }

    public void OnGet()
    {
        CollectionRevisions = CollectionRevisionMetadata.All(_connection.Db);
    }
}
