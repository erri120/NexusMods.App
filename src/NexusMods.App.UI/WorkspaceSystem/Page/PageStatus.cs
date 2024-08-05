namespace NexusMods.App.UI.WorkspaceSystem;

[Flags]
public enum PageStatus : byte
{
    /// <summary>
    /// The page is open.
    /// </summary>
    Open = 0b0000_0001,

    /// <summary>
    /// The page is open and visible.
    /// </summary>
    Visible = 0b0000_0011,
}
