using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Atom.UI.Forms.WpfBuilders;
using Genius.Starlog.UI.ViewModels;

namespace Genius.Starlog.UI.AutoGridBuilders;

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
                    .AddText(nameof(ProfileViewModel.Name), x => x.Filterable())
                    .AddText(nameof(ProfileViewModel.Path))
                    .AddCommand(nameof(ProfileViewModel.LoadProfileCommand), x => x
                        .WithIcon("Play32")
                        .WithStyle(new StylingRecord(Padding: new Thickness(0))))
            )
            .Build();
    }
}
