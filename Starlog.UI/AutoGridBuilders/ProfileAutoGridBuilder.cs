using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.AutoGridBuilders;

[ExcludeFromCodeCoverage]
public sealed class ProfileAutoGridBuilder : IAutoGridBuilder
{
    private readonly IAutoGridContextBuilder<ProfileViewModel> _contextBuilder;

    public ProfileAutoGridBuilder(IAutoGridContextBuilder<ProfileViewModel> contextBuilder)
    {
        _contextBuilder = contextBuilder.NotNull();
    }

    public AutoGridBuildContext Build()
    {
        return _contextBuilder
            .WithColumns(columns =>
                columns
                    .AddText(x => x.Name, x => x.Filterable())
                    .AddText(x => x.Path)
                    .AddCommand(x => x.LoadProfileCommand, x => x
                        .WithDisplayName(string.Empty)
                        .WithIcon("Play32")
                        .WithStyle(new StylingRecord(Padding: new Thickness(0))))
            )
            .Build();
    }
}
