using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;

namespace NexusMods.Games.StardewValley;

public static class Constants
{
    public static readonly RelativePath ModsFolder = "Mods";
    public static readonly RelativePath ContentFolder = "Content";
    public static readonly RelativePath ManifestFile = "manifest.json";

    public static readonly GamePath ModsGamePath = new(LocationId.Game, ModsFolder);
}
