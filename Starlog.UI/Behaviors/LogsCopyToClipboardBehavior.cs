using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using Genius.Starlog.UI.ViewModels;
using Microsoft.Xaml.Behaviors;

namespace Genius.Starlog.UI.Behaviors;

public sealed class LogsCopyToClipboardBehavior : Behavior<DataGrid>
{
    protected override void OnAttached()
    {
        AssociatedObject.ClipboardCopyMode = DataGridClipboardCopyMode.None;

        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.C || !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            return;
        }

        var itemGroups = AssociatedObject.SelectedItems.OfType<LogItemViewModel>()
            .GroupBy(x => x.File);
        StringBuilder sb = new();
        foreach (var itemGroup in itemGroups)
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("---------------------------------------");
                sb.AppendLine();
            }

            sb.Append("File: ").AppendLine(itemGroup.Key);
            foreach (var fileArtifact in itemGroup.First().Record.FileArtifacts.Artifacts)
            {
                sb.AppendLine(fileArtifact);
            }

            bool addSpace = false;
            foreach (var item in itemGroup)
            {
                if (addSpace)
                {
                    sb.AppendLine();
                }
                var d = item.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                sb.AppendLine($"{d}\t\t{item.Level}\t{item.Logger}\tThread {item.Thread}\t{item.Message}");

                if (!string.IsNullOrEmpty(item.Record.LogArtifacts))
                {
                    sb.AppendLine(item.Record.LogArtifacts);
                    addSpace = true;
                }
            }
        }

        Clipboard.SetText(sb.ToString());
        e.Handled = true;
    }
}
