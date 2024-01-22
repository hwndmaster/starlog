using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.AutoGridBuilders;

[ExcludeFromCodeCoverage]
public sealed class MessageParsingTestBuilder : IAutoGridBuilder
{
    private readonly IAutoGridContextBuilder<MessageParsingTestViewModel, MessageParsingViewModel> _contextBuilder;

    public MessageParsingTestBuilder(IAutoGridContextBuilder<MessageParsingTestViewModel, MessageParsingViewModel> contextBuilder)
    {
        _contextBuilder = contextBuilder.NotNull();
    }

    public IAutoGridContextBuilder Build()
    {
        return _contextBuilder
            .WithColumns(columns =>
                columns.AddDynamic(x => x.TestColumns, x => x.Entries)
            );
    }
}
