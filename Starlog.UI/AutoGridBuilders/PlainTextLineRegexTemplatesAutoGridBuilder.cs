using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Views.Generic;

namespace Genius.Starlog.UI.AutoGridBuilders;

[ExcludeFromCodeCoverage]
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
                    .AddText(x => x.Name)
                    .AddText(x => x.Regex)
                    .AddCommand(x => x.DeleteCommand, x => x
                        .WithIcon(ImageStock.Trash16)
                        .WithDisplayName(string.Empty))
            )
            .Build();
    }
}
