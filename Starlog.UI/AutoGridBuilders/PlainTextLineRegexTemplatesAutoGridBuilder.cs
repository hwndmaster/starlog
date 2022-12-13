using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.UI.ViewModels;

namespace Genius.Starlog.UI.AutoGridBuilders;

internal sealed class PlainTextLineRegexTemplatesAutoGridBuilder : IAutoGridBuilder
{
    private readonly IAutoGridContextBuilder<RegexValueViewModel> _contextBuilder;

    public PlainTextLineRegexTemplatesAutoGridBuilder(IAutoGridContextBuilder<RegexValueViewModel> contextBuilder)
    {
        _contextBuilder = contextBuilder.NotNull();
    }

    public AutoGridBuildContext Build()
    {
        return _contextBuilder
            .WithColumns(columns =>
                columns
                    .AddText(nameof(RegexValueViewModel.Name))
                    .AddText(nameof(RegexValueViewModel.Regex))
                    .AddCommand(nameof(RegexValueViewModel.DeleteCommand), x => x
                        .WithIcon("Trash16"))
            )
            .Build();
    }
}
