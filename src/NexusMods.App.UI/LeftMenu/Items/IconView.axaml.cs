using System.Reactive.Disposables;
using Avalonia.Controls.Metadata;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using Reloaded.Memory.Extensions;

namespace NexusMods.App.UI.LeftMenu.Items;

[UsedImplicitly]
[PseudoClasses(":page-open", ":page-visible")]
public partial class IconView : ReactiveUserControl<IIconViewModel>
{
    public IconView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Name)
                .BindTo(this, view => view.NameText.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Icon)
                .BindTo(this, view => view.LeftIcon.Value)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.NavigateCommand, view => view.ItemButton)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.Badges, view => view.Badges.ItemsSource)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.PageStatus)
                .SubscribeWithErrorLogging(status =>
                {
                    PseudoClasses.Remove(":page-open");
                    PseudoClasses.Remove(":page-visible");
                    if (!status.HasValue) return;

                    var value = status.Value;
                    if (value.HasFlagFast(PageStatus.Visible))
                    {
                        PseudoClasses.Add(":page-visible");
                    } else if (value.HasFlagFast(PageStatus.Open))
                    {
                        PseudoClasses.Add(":page-open");
                    }
                })
                .DisposeWith(d);
        });
    }
}

