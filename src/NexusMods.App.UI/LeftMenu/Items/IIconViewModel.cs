using System.Reactive;
using DynamicData.Kernel;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public interface IIconViewModel : ILeftMenuItemViewModel
{
    public string Name { get; set; }
    public IconValue Icon { get; set; }
    public string[] Badges { get; set; }
    public ReactiveCommand<NavigationInformation, Unit> NavigateCommand { get; set; }
    public Optional<PageStatus> PageStatus { get; set; }
}
