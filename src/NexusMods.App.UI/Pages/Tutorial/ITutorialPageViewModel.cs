using System.Reactive;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Tutorial;

public interface ITutorialPageViewModel : IPageViewModelInterface
{
    public bool InTutorial { get; }

    public int CurrentStep { get; }
    public int MaxSteps { get; set; }

    public ReactiveCommand<Unit, Unit> BeginTutorialCommand { get; }

    public ReactiveCommand<Unit, int> NextStepCommand { get; }

    public ReactiveCommand<Unit, int> BackStepCommand { get; }

    public ReactiveCommand<Unit, Unit> EndTutorialCommand { get; }
}
