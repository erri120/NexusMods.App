using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Tutorial;

[UsedImplicitly]
public partial class TutorialPageView : ReactiveUserControl<ITutorialPageViewModel>
{
    public TutorialPageView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.BeginTutorialCommand, view => view.BeginTutorialButton)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.InTutorial)
                .Subscribe(value =>
                {
                    TutorialBorder.IsVisible = value;
                    OverlayControl.IsVisible = value;
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.Control1.Bounds)
                .Select(_ => Control1.GetTransformedBounds())
                .Where(bounds => bounds.HasValue)
                .Select(bounds => bounds!.Value.Clip)
                .Select(rect => new AvaloniaList<Rect>
                {
                    rect.Inflate(thickness: 5.0),
                })
                .BindToView(this, view => view.OverlayControl.Masks)
                .DisposeWith(disposables);
        });
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        OverlayControl.Width = finalSize.Width;
        OverlayControl.Height = finalSize.Height;

        ContentBorder.Width = finalSize.Width;
        ContentBorder.Height = finalSize.Height;

        return base.ArrangeOverride(finalSize);
    }
}

public class CustomControl : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<Border, IBrush?>(nameof(Background));

    public static readonly StyledProperty<AvaloniaList<Rect>?> MasksProperty =
        AvaloniaProperty.Register<CustomControl, AvaloniaList<Rect>?>(nameof(Masks));

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public AvaloniaList<Rect>? Masks
    {
        get => GetValue(MasksProperty);
        set => SetValue(MasksProperty, value);
    }

    static CustomControl()
    {
        AffectsRender<CustomControl>(BoundsProperty, BackgroundProperty, MasksProperty);
    }

    public CustomControl()
    {
        Focusable = false;
        IsEnabled = false;
    }

    public override void Render(DrawingContext context)
    {
        var baseGeometry = new RectangleGeometry(new Rect(0, 0, Width, Height));
        Geometry clipGeometry = baseGeometry;

        var masks = Masks;
        if (masks is not null)
        {
            foreach (var mask in masks)
            {
                clipGeometry = Geometry.Combine(clipGeometry, new RectangleGeometry(mask), GeometryCombineMode.Exclude);
            }
        }

        using (context.PushGeometryClip(clipGeometry))
        {
            context.FillRectangle(Background ?? Brushes.Transparent, new Rect(0, 0, Width, Height));
        }
    }
}
