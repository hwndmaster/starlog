using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.Views;

public sealed class ShareLogsViewModel : ViewModelBase
{
    public ShareLogsViewModel(IReadOnlyCollection<ILogItemViewModel> items, IActionCommand closeCommand)
    {
        ShareContent = CopyToClipboardHelper.CreateLogsStringForClipboard(items);
        CloseCommand = closeCommand;

        CopyToClipboardHelper.CopyToClipboard(ShareContent);
    }

    public string ShareContent { get; }
    public IActionCommand CloseCommand { get; }
}
