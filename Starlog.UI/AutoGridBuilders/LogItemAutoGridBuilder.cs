using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.AutoGridBuilders;

[ExcludeFromCodeCoverage]
public sealed class LogItemAutoGridBuilder : IAutoGridBuilder
{
    private readonly IAutoGridContextBuilder<LogItemViewModel> _contextBuilder;

    public LogItemAutoGridBuilder(IAutoGridContextBuilder<LogItemViewModel> contextBuilder)
    {
        _contextBuilder = contextBuilder.NotNull();
    }

    public AutoGridBuildContext Build()
    {
        return _contextBuilder
            .WithColumns(columns =>
                columns
                    .AddToggleButton(x => x.IsBookmarked, x => x
                        .WithDisplayName(string.Empty)
                        .WithIcons("BookmarkOn32", "BookmarkOff32")
                        .WithStyle(new StylingRecord(Padding: new Thickness(2))))
                    .AddText(x => x.DateTime, x => x.WithDisplayFormat("yyyy-MM-dd HH:mm:ss.fff"))
                    .AddText(x => x.Level)
                    .AddText(x => x.Thread)
                    .AddText(x => x.File, x => x.WithVisibility(nameof(LogsViewModel.IsFileColumnVisible)))
                    .AddText(x => x.Logger, x => x.WithToolTipPath(nameof(LogItemViewModel.Logger)))
                    .AddText(x => x.Message, x => x
                        .WithToolTipPath(nameof(LogItemViewModel.Message))
                        .WithIconSource(new IconSourceRecord(nameof(LogItemViewModel.ArtifactsIcon)))
                        .WithAutoWidth(true)
                        .WithTextHighlighting(nameof(LogsViewModel.SearchText), nameof(LogsViewModel.SearchUseRegex))
                        )
            )
            .Build();
    }
}
