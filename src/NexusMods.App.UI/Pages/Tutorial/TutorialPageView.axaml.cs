using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using DynamicData;
using DynamicData.Kernel;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Tutorial;

[UsedImplicitly]
public partial class TutorialPageView : ReactiveUserControl<ITutorialPageViewModel>
{
    private Control[] _controlsToHighlight;

    private string[] _texts =
    [
        "This text is super cool!",
        "This button does awesome things!"
    ];

    public TutorialPageView()
    {
        InitializeComponent();
        _controlsToHighlight =
        [
            Control1,
            Control2,
        ];

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Subscribe(vm => vm.MaxSteps = _controlsToHighlight.Length)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.BeginTutorialCommand, view => view.BeginTutorialButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.NextStepCommand, view => view.TutorialNextButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.BackStepCommand, view => view.TutorialBackButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.EndTutorialCommand, view => view.TutorialCloseButton)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.InTutorial)
                .Subscribe(value =>
                {
                    TutorialCanvas.IsVisible = value;
                    OverlayControl.IsVisible = value;
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.CurrentStep)
                .Where(x => 0 <= x && x < _controlsToHighlight.Length)
                .Select(i => _controlsToHighlight[i])
                .Select(GetList)
                .Subscribe(masks => OverlayControl.Masks = masks)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.CurrentStep)
                .Where(x => 0 <= x && x < _texts.Length)
                .Select(i => _texts[i])
                .Subscribe(text => TutorialText.Text = text)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.CurrentStep, view => view.ViewModel!.MaxSteps)
                .Select(tuple =>
                {
                    var (current, max) = tuple;
                    return $"{current + 1}/{max}";
                })
                .Subscribe(text => TutorialStepCounter.Text = text)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.Control1.Bounds, view => view.ViewModel!.CurrentStep)
                .Select(tuple =>
                {
                    var index = _controlsToHighlight.IndexOf(Control1);
                    return tuple.Item2 == index ? GetList(Control1) : null;
                })
                .WhereNotNull()
                .Subscribe(masks => OverlayControl.Masks = masks)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.Control2.Bounds, view => view.ViewModel!.CurrentStep)
                .Select(tuple =>
                {
                    var index = _controlsToHighlight.IndexOf(Control2);
                    return tuple.Item2 == index ? GetList(Control2) : null;
                })
                .WhereNotNull()
                .Subscribe(masks => OverlayControl.Masks = masks)
                .DisposeWith(disposables);
        });
    }

    private static AvaloniaList<Rect>? GetList(Control control)
    {
        var clip = GetClip(control);
        return clip.HasValue
            ? new AvaloniaList<Rect>
            {
                clip.Value,
            }
            : null;
    }

    private static Optional<Rect> GetClip(Control control)
    {
        var transformedBounds = control.GetTransformedBounds();
        if (!transformedBounds.HasValue) return Optional<Rect>.None;
        return transformedBounds.Value.Clip;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        TutorialCanvas.Width = finalSize.Width;
        TutorialCanvas.Height = finalSize.Height;

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
