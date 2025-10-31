using Microsoft.AspNetCore.Mvc.RazorPages;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.App.Server.Pages;

public class FilesPage : PageModel
{
    private readonly IConnection _connection;

    public Entities<LoadoutFile.ReadOnly> Files { get; set; }
    
    public FilesPage(IConnection connection)
    {
        _connection = connection;
    }

    public void OnGet()
    {
        Files = LoadoutFile.All(_connection.Db);
    }
}
