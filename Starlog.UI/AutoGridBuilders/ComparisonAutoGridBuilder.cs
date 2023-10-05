using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.UI.Views.Comparison;

namespace Genius.Starlog.UI.AutoGridBuilders;

[ExcludeFromCodeCoverage]
public sealed class ComparisonAutoGridBuilder : IAutoGridBuilder
{
    private readonly IAutoGridContextBuilder<LogItemPairViewModel> _contextBuilder;

    public ComparisonAutoGridBuilder(IAutoGridContextBuilder<LogItemPairViewModel> contextBuilder)
    {
        _contextBuilder = contextBuilder.NotNull();
    }

    public AutoGridBuildContext Build()
    {
        return _contextBuilder
            .WithColumns(columns =>
                columns
                    .AddText(x => x.Thread1)
                    .AddText(x => x.File1)
                    .AddText(x => x.Logger1, x => x.WithToolTipPath(nameof(LogItemPairViewModel.Logger1)))
                    .AddText(x => x.Message1, x => x
                        .WithToolTipPath(nameof(LogItemPairViewModel.Message1))
                        .WithIconSource(new IconSourceRecord(nameof(LogItemPairViewModel.ArtifactsIcon1)))
                        .WithAutoWidth(true))
                    .AddText(x => x.Thread2)
                    .AddText(x => x.File2)
                    .AddText(x => x.Logger2, x => x.WithToolTipPath(nameof(LogItemPairViewModel.Logger2)))
                    .AddText(x => x.Message2, x => x
                        .WithToolTipPath(nameof(LogItemPairViewModel.Message2))
                        .WithIconSource(new IconSourceRecord(nameof(LogItemPairViewModel.ArtifactsIcon2)))
                        .WithAutoWidth(true))
            )
            .Build();
    }
}
