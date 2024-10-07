using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Controls;

public interface ITreeTestingViewModel : IViewModelInterface
{
    ITreeDataGridSource? TreeDataGridSource { get; }
}

public class TreeTestingViewModel : AViewModel<ITreeTestingViewModel>, ITreeTestingViewModel
{
    public ITreeDataGridSource? TreeDataGridSource { get; }

    public TreeTestingViewModel()
    {
        var source = new FlatTreeDataGridSource<IModelType>([
            new ModelType1(),
            new ModelType2(),
            new ModelType1(),
            new ModelType2(),
            new ModelType1(),
            new ModelType3(),
        ]);

        source.Columns.AddRange([
            new TemplateColumn<IModelType>(
                header: "Foo",
                cellTemplateResourceKey: "OtherTemplate",
                options: new TemplateColumnOptions<IModelType>
                {
                    CompareAscending = static (a, b) => a?.CompareTo(b) ?? 0,
                    CompareDescending = static (a, b) => b?.CompareTo(a) ?? 0,
                }
            ),
        ]);

        var c = new MyTemplateColumn<IModelType>(
            header: "Bar",
            options: new TemplateColumnOptions<IModelType>()
        )
        {
            FallbackTemplateKey = "FallbackThingy",
        };

        c.AddType<ModelType1>(key: "Thing1");
        c.AddType<ModelType2>(key: "Thing2");

        source.Columns.Add(c);
        TreeDataGridSource = source;
    }
}

public class MyTemplateColumn<TModel> : ColumnBase<TModel>
    where TModel : notnull
{
    private readonly Dictionary<Type, string> _typeToDataTemplateKey = new();
    private readonly Dictionary<Type, IDataTemplate> _typeToDataTemplate = new();

    private IDataTemplate? _fallbackTemplate;
    public string? FallbackTemplateKey { get; init; }

    public MyTemplateColumn(
        object? header,
        GridLength? width = null,
        TemplateColumnOptions<TModel>? options = null)
        : base(header, width, options ?? new TemplateColumnOptions<TModel>()) { }

    public void AddType<T>(string key) where T : TModel
    {
        _typeToDataTemplateKey[typeof(T)] = key;
    }

    public override ICell CreateCell(IRow<TModel> row)
    {
        var cell = new TemplateCell(
            value: row.Model,
            getCellTemplate: control => GetCellTemplate(control, row.Model.GetType()),
            getCellEditingTemplate: null,
            options: Options as TemplateColumnOptions<TModel>
        );

        return cell;
    }

    private IDataTemplate GetCellTemplate(Control anchor, Type key)
    {
        if (_typeToDataTemplate.TryGetValue(key, out var value)) return value;
        if (_typeToDataTemplateKey.TryGetValue(key, out var resourceKey))
        {
            value = anchor.FindResource(resourceKey) as IDataTemplate;
            _typeToDataTemplate[key] = value ?? throw new KeyNotFoundException();
            return value;
        }

        if (FallbackTemplateKey is not null)
        {
            if (_fallbackTemplate is not null) return _fallbackTemplate;
            _fallbackTemplate = anchor.FindResource(FallbackTemplateKey) as IDataTemplate;
            if (_fallbackTemplate is null) throw new KeyNotFoundException();
            return _fallbackTemplate;
        }

        throw new KeyNotFoundException();
    }

    public override Comparison<TModel?>? GetComparison(ListSortDirection direction)
    {
        return direction switch
        {
            ListSortDirection.Ascending => Options.CompareAscending,
            ListSortDirection.Descending => Options.CompareDescending,
        };
    }
}

public class MyTemplate : IRecyclingDataTemplate
{
    [Content]
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global", Justification = "Updated in XAML")]
    public Dictionary<Type, IRecyclingDataTemplate> AvailableTemplates { get; } = new();

    public IRecyclingDataTemplate? FallbackTemplate { get; set; }

    public bool Match(object? data)
    {
        if (data is null) return false;

        var key = data.GetType();
        return AvailableTemplates.ContainsKey(key) || FallbackTemplate is not null;
    }

    public Control? Build(object? data) => Build(data, existing: null);

    public Control? Build(object? data, Control? existing)
    {
        ArgumentNullException.ThrowIfNull(data);
        var key = data.GetType();

        if (AvailableTemplates.TryGetValue(key, out var value)) return value.Build(data, existing: existing);
        if (FallbackTemplate is not null) return FallbackTemplate.Build(data, existing: existing);
        throw new KeyNotFoundException($"Key not found: {key}");
    }
}

public interface IModelType : IComparable<IModelType>;

public record ModelType1 : IModelType
{
    public int CompareTo(IModelType? other)
    {
        if (other is not ModelType1) return -1;
        return 0;
    }
}

public record ModelType2 : IModelType
{
    public int CompareTo(IModelType? other)
    {
        if (other is not ModelType2) return -1;
        return 0;
    }
}

public record ModelType3 : IModelType
{
    public int CompareTo(IModelType? other)
    {
        if (other is not ModelType3) return -1;
        return 0;
    }
}

[UsedImplicitly]
public partial class TreeTesting : ReactiveUserControl<ITreeTestingViewModel>
{
    public TreeTesting()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.TreeDataGridSource, view => view.TreeDataGrid.Source)
                .DisposeWith(disposables);
        });
    }
}

