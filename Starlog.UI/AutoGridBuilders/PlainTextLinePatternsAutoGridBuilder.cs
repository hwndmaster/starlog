using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.AutoGridBuilders;

[ExcludeFromCodeCoverage]
internal sealed class PlainTextLinePatternsAutoGridBuilder : IAutoGridBuilder
{
    private readonly IAutoGridContextBuilder<PatternValueViewModel, IViewModel> _contextBuilder;

    public PlainTextLinePatternsAutoGridBuilder(IAutoGridContextBuilder<PatternValueViewModel, IViewModel> contextBuilder)
    {
        _contextBuilder = contextBuilder.NotNull();
    }

    public IAutoGridContextBuilder Build()
    {
        return _contextBuilder
            .WithColumns(columns =>
                columns
                    .AddText(x => x.Name)
                    .AddText(x => x.Type)
                    .AddText(x => x.Pattern)
                    .AddCommand(x => x.DeleteCommand, opts => opts
                        .WithIcon(ImageStock.Trash16)
                        .WithDisplayName(string.Empty))
            );
    }
}
