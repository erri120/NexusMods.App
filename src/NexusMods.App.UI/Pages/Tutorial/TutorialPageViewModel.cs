using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Tutorial;

public class TutorialPageViewModel : APageViewModel<ITutorialPageViewModel>, ITutorialPageViewModel
{
    public TutorialPageViewModel(IWindowManager windowManager) : base(windowManager) { }
}
