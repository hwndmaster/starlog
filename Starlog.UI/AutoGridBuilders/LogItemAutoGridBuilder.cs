using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.AutoGridBuilders;

[ExcludeFromCodeCoverage]
public sealed class LogItemAutoGridBuilder : IAutoGridBuilder
{
    private readonly IAutoGridContextBuilder<LogItemViewModel, LogsViewModel> _contextBuilder;

    public LogItemAutoGridBuilder(IAutoGridContextBuilder<LogItemViewModel, LogsViewModel> contextBuilder)
    {
        _contextBuilder = contextBuilder.NotNull();
    }

    public IAutoGridContextBuilder Build()
    {
        return _contextBuilder
            .WithColumns(columns =>
                columns
                    .AddToggleButton(x => x.IsBookmarked, x => x
                        .WithDisplayName(string.Empty)
                        .WithIcons("BookmarkOn32", "BookmarkOff32")
                        .WithStyle(new StylingRecord(Padding: new Thickness(2))))
                    .AddText(x => x.DateTime, opts => opts.WithDisplayFormat("yyyy-MM-dd HH:mm:ss.fff"))
                    .AddText(x => x.Level)
                    .AddDynamic(x => x.FieldColumns, x => x.FieldEntries)
                    .AddText(x => x.Source, opts => opts.WithVisibility(x => x.IsFileColumnVisible))
                    .AddText(x => x.Message, opts => opts
                        .WithToolTipPath(x => x.Message)
                        .WithIconSource(new IconSourceRecord<LogItemViewModel>(x => x.ArtifactsIcon))
                        .WithAutoWidth(true)
                        .WithTextHighlighting(y => y.SearchText, y => y.SearchUseRegex)
                        )
                    .AddDynamic(x => x.MessageParsingColumns, x => x.MessageParsingEntries)
            );
    }
}
