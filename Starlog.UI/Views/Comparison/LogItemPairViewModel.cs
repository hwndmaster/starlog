namespace Genius.Starlog.UI.Views.Comparison;

public sealed class LogItemPairViewModel : ViewModelBase
{
    private readonly LogItemViewModel _item1;
    private readonly LogItemViewModel _item2;

    public LogItemPairViewModel(LogItemViewModel item1, LogItemViewModel item2)
    {
        _item1 = item1.NotNull();
        _item2 = item2.NotNull();
    }

    // TODO: public string Thread1 => _item1.Thread;
    public string Source1 => _item1.Source;
    // TODO: public string Logger1 => _item1.Logger;
    public string Message1 => _item1.Message;
    public string? ArtifactsIcon1 => string.IsNullOrEmpty(_item1.Record.LogArtifacts) ? null : "Note32";

    // TODO: public string Thread2 => _item2.Thread;
    public string Source2 => _item2.Source;
    // TODO: public string Logger2 => _item2.Logger;
    public string Message2 => _item2.Message;
    public string? ArtifactsIcon2 => string.IsNullOrEmpty(_item2.Record.LogArtifacts) ? null : "Note32";
}
