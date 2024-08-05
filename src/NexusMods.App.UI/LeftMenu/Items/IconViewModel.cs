using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class IconViewModel : AViewModel<IIconViewModel>, IIconViewModel
{
    [Reactive] public string Name { get; set; } = "";

    [Reactive] public IconValue Icon { get; set; } = new();

    [Reactive] public string[] Badges { get; set; } = [];

    [Reactive] public ReactiveCommand<NavigationInformation, Unit> NavigateCommand { get; set; } = ReactiveCommand.Create<NavigationInformation>(_ => { }, Observable.Return(true));

    [Reactive] public Optional<PageStatus> PageStatus { get; set; }

    public IconViewModel()
    {
        
    }

    public IconViewModel(
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId,
        Func<IPageFactoryContext, bool> pageContextPredicate)
    {
        this.WhenActivated(disposables =>
        {
            if (!workspaceController.TryGetWorkspace(workspaceId, out var workspace)) return;

            workspace
                .ObservePageStatus(pageContextPredicate)
                .BindToVM(this, vm => vm.PageStatus)
                .DisposeWith(disposables);
        });
    }
}
