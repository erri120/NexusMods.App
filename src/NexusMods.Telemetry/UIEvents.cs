using JetBrains.Annotations;

namespace NexusMods.Telemetry;

/// <summary>
///
/// </summary>
[PublicAPI]
public static class UIEvents
{
    public static readonly EventDefinition AddLoadout = new(Category: "Loadout", Action: "Add Loadout");
    public static readonly EventDefinition RemoveLoadout = new(Category: "Loadout", Action: "Remove Loadout");
    public static readonly EventDefinition ApplyLoadout = new(Category: "Loadout", Action: "Apply Loadout");

    public static readonly EventDefinition ManageGame = new(Category: "Game Management", Action: "Manage Game");
    public static readonly EventDefinition UnmanageGame = new(Category: "Game Management", Action: "Unmanage Game");

    public static readonly EventDefinition Foo = new(Category: "Health Check", Action: "Open Diagnostic");
    public static readonly EventDefinition MyNewEvent = new(Category: "Collections", Action: "Install All");
}
