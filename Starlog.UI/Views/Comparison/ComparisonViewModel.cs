using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.UI.AutoGridBuilders;

namespace Genius.Starlog.UI.Views.Comparison;

public interface IComparisonViewModel : ITabViewModel
{
    void PopulateProfiles(ComparisonContext comparisonContext);
}

// TODO: Cover with unit tests
public sealed class ComparisonViewModel : TabViewModelBase, IComparisonViewModel
{
    private ComparisonContext? _context;

    public ComparisonViewModel(
        ComparisonAutoGridBuilder autoGridBuilder
    )
    {
        // Dependencies:
        AutoGridBuilder = autoGridBuilder.NotNull();

        // Actions:
        // ...

        // Subscriptions:
        // ...
    }

    public void PopulateProfiles(ComparisonContext comparisonContext)
    {
        _context = comparisonContext.NotNull();

        Profile1Name = _context.Profile1.Name;
        Profile1Path = _context.Profile1.Settings.Source;
        Profile2Name = _context.Profile2.Name;
        Profile2Path = _context.Profile2.Settings.Source;

        // TODO: Further load
    }

    public IAutoGridBuilder AutoGridBuilder { get; }

    public string Profile1Name
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public string Profile1Path
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public string Profile2Name
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    public string Profile2Path
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }
}
