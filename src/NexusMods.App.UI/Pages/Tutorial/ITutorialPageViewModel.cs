using System.Reactive;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Tutorial;

public interface ITutorialPageViewModel : IPageViewModelInterface
{
    public bool InTutorial { get; }

    public ReactiveCommand<Unit, Unit> BeginTutorialCommand { get; set; }
}
