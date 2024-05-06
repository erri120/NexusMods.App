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
    public ReactiveCommand<Unit, Unit> BeginTutorialCommand { get; set; }

    public TutorialPageViewModel() : this(DesignWindowManager.Instance) { }

    public TutorialPageViewModel(IWindowManager windowManager) : base(windowManager)
    {
        BeginTutorialCommand = ReactiveCommand.Create(() =>
        {
            InTutorial = true;
        }, this.WhenAnyValue(vm => vm.InTutorial, x=> !x));
    }
}
