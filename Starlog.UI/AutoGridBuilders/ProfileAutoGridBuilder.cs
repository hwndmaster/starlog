using System.Diagnostics.CodeAnalysis;
using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Atom.UI.Forms.Wpf;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.AutoGridBuilders;

[ExcludeFromCodeCoverage]
public sealed class ProfileAutoGridBuilder : IAutoGridBuilder
{
    private readonly IAutoGridContextBuilder<ProfileViewModel, ProfilesViewModel> _contextBuilder;

    public ProfileAutoGridBuilder(IAutoGridContextBuilder<ProfileViewModel, ProfilesViewModel> contextBuilder)
    {
        _contextBuilder = contextBuilder.NotNull();
    }

    public IAutoGridContextBuilder Build()
    {
        return _contextBuilder
            .WithColumns(columns =>
                columns
                    .AddText(x => x.Name, opts => opts.Filterable())
                    .AddText(x => x.Source)
                    .AddText(x => x.LastOpened)
                    .AddCommand(x => x.LoadProfileCommand, opts => opts
                        .WithDisplayName(string.Empty)
                        .WithIcon("Play32")
                        .WithStyle(new StylingRecord(Padding: new Thickness(0))))
            );
    }
}
