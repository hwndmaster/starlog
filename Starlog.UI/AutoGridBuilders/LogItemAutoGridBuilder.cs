using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
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
                    .AddText(x => x.DateTime, x => x.WithDisplayFormat("yyyy-MM-dd HH:mm:ss.fff"))
                    .AddText(x => x.Level)
                    .AddText(x => x.Thread)
                    .AddText(x => x.File)
                    .AddText(x => x.Logger, x => x.WithToolTipPath(nameof(LogItemViewModel.Logger)))
                    .AddText(x => x.Message, x => x
                        .WithToolTipPath(nameof(LogItemViewModel.Message))
                        .WithIconSource(new IconSourceRecord(nameof(LogItemViewModel.ArtifactsIcon)))
                        .WithAutoWidth(true))
            )
            .Build();
    }
}
