using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;

namespace Genius.Starlog.UI.Views;

[ExcludeFromCodeCoverage]
public partial class LogsView
{
    public LogsView()
    {
        InitializeComponent();

        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object? _, RoutedEventArgs args)
    {
        this.Loaded -= OnLoaded;

        ((GridSplitter)this.FindName("ArtifactsSplitter")).DragCompleted += (_, e) =>
        {
            ((RichTextBox)this.FindName("Artifacts")).MaxHeight = double.PositiveInfinity;
        };
    }
}
