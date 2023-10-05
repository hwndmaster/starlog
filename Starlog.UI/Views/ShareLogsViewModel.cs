using Genius.Starlog.UI.Helpers;

namespace Genius.Starlog.UI.Views;

// TODO: Cover with unit tests
public sealed class ShareLogsViewModel : ViewModelBase
{
    public ShareLogsViewModel(IClipboardHelper clipboardHelper, IReadOnlyCollection<ILogItemViewModel> items, IActionCommand closeCommand)
    {
        Guard.NotNull(clipboardHelper);

        ShareContent = clipboardHelper.CreateLogsStringForClipboard(items);
        CloseCommand = closeCommand;

        clipboardHelper.CopyToClipboard(ShareContent);
    }

    public string ShareContent { get; }
    public IActionCommand CloseCommand { get; }
}
