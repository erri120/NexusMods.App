using System.Reactive;
using JetBrains.Annotations;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Tutorial;

[UsedImplicitly]
public class TutorialPageViewModel : APageViewModel<ITutorialPageViewModel>, ITutorialPageViewModel
{
    [Reactive] public bool InTutorial { get; private set; }

    [Reactive] public int CurrentStep { get; private set; } = -1;
    [Reactive] public int MaxSteps { get; set; }

    public ReactiveCommand<Unit, Unit> BeginTutorialCommand { get; }
    public ReactiveCommand<Unit, int> NextStepCommand { get; }
    public ReactiveCommand<Unit, int> BackStepCommand { get; }
    public ReactiveCommand<Unit, Unit> EndTutorialCommand { get; }

    public TutorialPageViewModel() : this(DesignWindowManager.Instance) { }

    public TutorialPageViewModel(IWindowManager windowManager) : base(windowManager)
    {
        BeginTutorialCommand = ReactiveCommand.Create(() =>
        {
            InTutorial = true;
            CurrentStep = 0;
        }, this.WhenAnyValue(vm => vm.InTutorial, x=> !x));

        EndTutorialCommand = ReactiveCommand.Create(() =>
        {
            InTutorial = false;
        }, this.WhenAnyValue(vm => vm.InTutorial));

        NextStepCommand = ReactiveCommand.Create(() =>
        {
            var nextStep = CurrentStep + 1;
            CurrentStep = nextStep;
            return CurrentStep;
        }, this.WhenAnyValue(vm => vm.CurrentStep, x => x + 1 < MaxSteps));

        BackStepCommand = ReactiveCommand.Create(() =>
        {
            var nextStep = CurrentStep - 1;
            CurrentStep = nextStep;
            return CurrentStep;
        }, this.WhenAnyValue(vm => vm.CurrentStep, x => x > 0));
    }
}
