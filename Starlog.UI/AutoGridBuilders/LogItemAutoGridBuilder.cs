using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Starlog.UI.ViewModels;

namespace Genius.Starlog.UI.AutoGridBuilders;

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
                    .AddText(nameof(LogItemViewModel.DateTime))
                    .AddText(nameof(LogItemViewModel.Level), x => x.Filterable())
                    .AddText(nameof(LogItemViewModel.Thread), x => x.Filterable())
                    .AddText(nameof(LogItemViewModel.Logger), x => x.Filterable())
                    .AddText(nameof(LogItemViewModel.Message), x => x.Filterable())
            )
            .Build();
    }
}
