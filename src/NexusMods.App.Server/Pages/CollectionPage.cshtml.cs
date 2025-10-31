using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.Server.Pages;

public class CollectionPage : PageModel
{
    private readonly IConnection _connection;

    public CollectionRevisionMetadata.ReadOnly Metadata { get; set; }

    public CollectionPage(IConnection connection)
    {
        _connection = connection;
    }
    
    public IActionResult OnGet(ulong id)
    {
        Metadata = CollectionRevisionMetadata.Load(_connection.Db, EntityId.From(id));
        if (!Metadata.IsValid()) return NotFound();

        return Page();
    }
}
