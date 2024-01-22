using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.UI.Views.Comparison;

namespace Genius.Starlog.UI.AutoGridBuilders;

[ExcludeFromCodeCoverage]
public sealed class ComparisonAutoGridBuilder : IAutoGridBuilder
{
    private readonly IAutoGridContextBuilder<LogItemPairViewModel, ComparisonViewModel> _contextBuilder;

    public ComparisonAutoGridBuilder(IAutoGridContextBuilder<LogItemPairViewModel, ComparisonViewModel> contextBuilder)
    {
        _contextBuilder = contextBuilder.NotNull();
    }

    public IAutoGridContextBuilder Build()
    {
        return _contextBuilder
            .WithColumns(columns =>
                columns
                    .AddText(x => x.Thread1)
                    .AddText(x => x.File1)
                    .AddText(x => x.Logger1, opts => opts.WithToolTipPath(x => x.Logger1))
                    .AddText(x => x.Message1, opts => opts
                        .WithToolTipPath(x => x.Message1)
                        .WithIconSource(new IconSourceRecord<LogItemPairViewModel>(y => y.ArtifactsIcon1))
                        .WithAutoWidth(true))
                    .AddText(x => x.Thread2)
                    .AddText(x => x.File2)
                    .AddText(x => x.Logger2, opts => opts.WithToolTipPath(x => x.Logger2))
                    .AddText(x => x.Message2, opts => opts
                        .WithToolTipPath(x => x.Message2)
                        .WithIconSource(new IconSourceRecord<LogItemPairViewModel>(y => y.ArtifactsIcon2))
                        .WithAutoWidth(true))
            );
    }
}
