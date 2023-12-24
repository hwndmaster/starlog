using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Views;
using Genius.Starlog.UI.Views.Generic;

namespace Genius.Starlog.UI.AutoGridBuilders;

[ExcludeFromCodeCoverage]
internal sealed class PlainTextLineRegexTemplatesAutoGridBuilder : IAutoGridBuilder
{
    private readonly IAutoGridContextBuilder<RegexValueViewModel, IViewModel> _contextBuilder;

    public PlainTextLineRegexTemplatesAutoGridBuilder(IAutoGridContextBuilder<RegexValueViewModel, IViewModel> contextBuilder)
    {
        _contextBuilder = contextBuilder.NotNull();
    }

    public IAutoGridContextBuilder Build()
    {
        return _contextBuilder
            .WithColumns(columns =>
                columns
                    .AddText(x => x.Name)
                    .AddText(x => x.Regex)
                    .AddCommand(x => x.DeleteCommand, opts => opts
                        .WithIcon(ImageStock.Trash16)
                        .WithDisplayName(string.Empty))
            );
    }
}
