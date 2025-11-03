using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.Server.Pages;

public class LoadoutPage(IConnection connection, ISynchronizerService synchronizerService) : PageModel
{
    public Loadout.ReadOnly Loadout { get; private set; }

    public IActionResult OnGet(ulong id)
    {
        var loadout = Abstractions.Loadouts.Loadout.Load(connection.Db, EntityId.From(id));
        if (!loadout.IsValid()) return NotFound();

        var synchronizer = loadout.InstallationInstance.GetGame().Synchronizer;
        return Page();
    }

    public async ValueTask<IActionResult> OnPostSynchronize(ulong id)
    {
        var loadout = Abstractions.Loadouts.Loadout.Load(connection.Db, EntityId.From(id));
        if (!loadout.IsValid()) return NotFound();

        var synchronizer = loadout.InstallationInstance.GetGame().Synchronizer;
        // await synchronizer.Synchronize(loadout);
        return Page();
    }
}
